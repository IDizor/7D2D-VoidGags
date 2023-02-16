using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_SaveLockedSlots(Harmony harmony)
        {
            if (IsUndeadLegacy)
            {
                Debug.Log($"Mod {nameof(VoidGags)}: Patch '{nameof(Settings.SaveLockedSlotsCount)}' is not applicable for Undead Legacy.");
                return;
            }

            harmony.Patch(AccessTools.Method(typeof(XUiC_ContainerStandardControls), "ChangeLockedSlots"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((long _newValue) => XUiC_ContainerStandardControls_ChangeLockedSlots.Postfix(_newValue))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_BackpackWindow), "Init"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_BackpackWindow __instance) => XUiC_BackpackWindow_Init.Postfix(__instance))));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.SaveLockedSlotsCount)}");
        }

        /// <summary>
        /// Saves locked slots count to file.
        /// </summary>
        public class XUiC_ContainerStandardControls_ChangeLockedSlots
        {
            public static void Postfix(long _newValue)
            {
                Helper.SaveLockedSlotsCount((int)_newValue);
            }
        }

        /// <summary>
        /// Applies saved locked slots count.
        /// </summary>
        public class XUiC_BackpackWindow_Init
        {
            public static void Postfix(XUiC_BackpackWindow __instance)
            {
                OnGameLoadedActions.Enqueue(() =>
                {
                    var lockedSlots = Helper.LoadLockedSlotsCount();
                    if (lockedSlots > 0)
                    {
                        // set value
                        XUiC_ContainerStandardControls backpackControls = __instance.GetChildByType<XUiC_ContainerStandardControls>();
                        if (backpackControls != null)
                        {
                            backpackControls.ChangeLockedSlots(lockedSlots);
                        }

                        // update UI
                        var cbx = __instance.GetChildById("cbxLockedSlots") as XUiC_ComboBoxInt;
                        if (cbx != null)
                        {
                            cbx.Value = lockedSlots;
                        }

                        Debug.Log($"Inventory locked slots applied: {lockedSlots}");
                    }
                });
            }
        }
    }
}
