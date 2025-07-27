using System.Linq;
using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_EnqueueCraftWhenNoFuel()
        {
            LogApplyingPatch(nameof(Settings.EnqueueCraftWhenNoFuel));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationFuelGrid), nameof(XUiC_WorkstationFuelGrid.HasRequirement)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_WorkstationFuelGrid __instance, bool __result) => XUiC_WorkstationFuelGrid_HasRequirement.Postfix(__instance, ref __result))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationFuelGrid), nameof(XUiC_WorkstationFuelGrid.TurnOn)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_WorkstationFuelGrid __instance) => XUiC_WorkstationFuelGrid_TurnOn.Postfix(__instance))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationWindowGroup), nameof(XUiC_WorkstationWindowGroup.OnClose)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_WorkstationWindowGroup __instance) => XUiC_WorkstationWindowGroup_OnClose.Postfix(__instance))));
        }

        /// <summary>
        /// Allow to enqueue item to craft when no fuel in the workstation.
        /// </summary>
        public class XUiC_WorkstationFuelGrid_HasRequirement
        {
            public static void Postfix(XUiC_WorkstationFuelGrid __instance, ref bool __result)
            {
                if (!__result && !__instance.HasFuelAndCanStart())
                {
                    __result = true;
                }
            }
        }

        /// <summary>
        /// Immediate turn-off the workstation when no fuel.
        /// </summary>
        public class XUiC_WorkstationFuelGrid_TurnOn
        {
            public static void Postfix(XUiC_WorkstationFuelGrid __instance)
            {
                if (__instance.isOn && !__instance.HasFuelAndCanStart())
                {
                    __instance.TurnOff();
                }
            }
        }

        /// <summary>
        /// Display warning when leaving workstation turned off.
        /// </summary>
        public class XUiC_WorkstationWindowGroup_OnClose
        {
            public static void Postfix(XUiC_WorkstationWindowGroup __instance)
            {
                if (IsDedicatedServer) return;

                // if it has fuel window
                if (__instance.fuelWindow != null)
                {
                    var warning = !__instance.fuelWindow.isOn;
                    warning &= __instance.craftingQueue?.queueItems.Any(i =>
                        ((XUiC_RecipeStack)i).recipe != null && ((XUiC_RecipeStack)i).recipeCount > 0) == true;

                    if (warning)
                    {
                        XUiC_PopupToolTip.QueueTooltip(Helper.PlayerLocal.PlayerUI.xui, $"{Localization.Get(__instance.workstation)} : {Localization.Get("TwitchAction_EmptyFuel")} / {Localization.Get("goDisabled")}",
                            _args: null, _alertSound: "batterybank_stop", _eventHandler: null, _showImmediately: false, _pinTooltip: false);
                    }
                }
            }
        }
    }
}
