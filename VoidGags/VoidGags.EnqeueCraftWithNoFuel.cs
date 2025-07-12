using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_EnqueueCraftWhenNoFuel(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationFuelGrid), nameof(XUiC_WorkstationFuelGrid.HasRequirement)), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_WorkstationFuelGrid __instance, bool __result) => XUiC_WorkstationFuelGrid_HasRequirement.Postfix(__instance, ref __result))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_WorkstationFuelGrid), nameof(XUiC_WorkstationFuelGrid.TurnOn)), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_WorkstationFuelGrid __instance) => XUiC_WorkstationFuelGrid_TurnOn.Postfix(__instance))));

            LogPatchApplied(nameof(Settings.EnqueueCraftWhenNoFuel));
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
    }
}
