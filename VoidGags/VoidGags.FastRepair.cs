using System;
using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.FastRepair;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_FastRepair()
        {
            LogApplyingPatch(nameof(Settings.FastRepair));

            if (!Enum.TryParse(Settings.FastRepair_HotKey, out FastRepairHotKey))
            {
                LogModException($"Invalid value for setting '{nameof(Settings.FastRepair_HotKey)}'.");
                return;
            }

            Harmony.Patch(AccessTools.Method(typeof(XUiC_Toolbelt), nameof(XUiC_Toolbelt.Update)),
                postfix: new HarmonyMethod(XUiC_Toolbelt_Update.Postfix));
        }

        public static class FastRepair
        {
            public static KeyCode FastRepairHotKey = KeyCode.Mouse2;
            public static bool RepairingFlag = false;

            /// <summary>
            /// Repairs current weapon/tool in hands by the hot key.
            /// </summary>
            public class XUiC_Toolbelt_Update
            {
                public static void Postfix(XUiC_Toolbelt __instance, XUiController[] ___itemControllers, int ___currentHoldingIndex)
                {
                    if (Input.GetKey(FastRepairHotKey))
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
}
