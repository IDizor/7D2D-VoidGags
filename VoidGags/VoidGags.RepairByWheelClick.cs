using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_RepairByWheelClick(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(XUiC_Toolbelt), "Update"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_Toolbelt_Update_Params p) =>
                XUiC_Toolbelt_Update.Postfix(p.__instance, p.___itemControllers, p.___currentHoldingIndex))));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.MouseWheelClickFastRepair)}");
        }

        private struct XUiC_Toolbelt_Update_Params
        {
            public XUiC_Toolbelt __instance;
            public XUiController[] ___itemControllers;
            public int ___currentHoldingIndex;
        }

        /// <summary>
        /// Repairs current weapon/tool in hands by mouse wheel click.
        /// </summary>
        [HarmonyPatch(typeof(XUiC_Toolbelt))]
        [HarmonyPatch("Update")]
        public class XUiC_Toolbelt_Update
        {
            private static bool RepairingFlag = false;

            public static void Postfix(XUiC_Toolbelt __instance, XUiController[] ___itemControllers, int ___currentHoldingIndex)
            {
                if (Input.GetMouseButton(2)) // Middle button pressed (wheel)
                {
                    if (!RepairingFlag)
                    {
                        RepairingFlag = true;

                        if (___currentHoldingIndex != __instance.xui.PlayerInventory.Toolbelt.DUMMY_SLOT_IDX)
                        {
                            var currentItem = (XUiC_ItemStack)___itemControllers[___currentHoldingIndex];
                            var itemValue = currentItem?.ItemStack?.itemValue;

                            if (itemValue != null && itemValue.MaxUseTimes > 0 && itemValue.UseTimes > 0f && itemValue.ItemClass.RepairTools != null && itemValue.ItemClass.RepairTools.Length > 0 && itemValue.ItemClass.RepairTools[0].Value.Length > 0)
                            {
                                var repairAction = new ItemActionEntryRepair(currentItem);
                                repairAction.RefreshEnabled();
                                if (repairAction.Enabled)
                                {
                                    repairAction.OnActivated();
                                }
                                else
                                {
                                    repairAction.OnDisabledActivate();
                                }
                            }
                        }
                    }

                }
                else
                {
                    RepairingFlag = false;
                }
            }
        }
    }
}
