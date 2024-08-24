using System;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        static KeyCode fastRepairHotKey = KeyCode.Mouse2;

        public void ApplyPatches_FastRepair(Harmony harmony)
        {
            if (!Enum.TryParse(Settings.FastRepair_HotKey, out fastRepairHotKey))
            {
                LogModException($"Invalid value for setting '{nameof(Settings.FastRepair_HotKey)}'.");
                return;
            }

            harmony.Patch(AccessTools.Method(typeof(XUiC_Toolbelt), "Update"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_Toolbelt_Update.APostfix p) =>
                XUiC_Toolbelt_Update.Postfix(p.__instance, p.___itemControllers, p.___currentHoldingIndex))));

            LogPatchApplied(nameof(Settings.FastRepair));
        }

        /// <summary>
        /// Repairs current weapon/tool in hands by the hot key.
        /// </summary>
        public class XUiC_Toolbelt_Update
        {
            private static bool RepairingFlag = false;

            public struct APostfix
            {
                public XUiC_Toolbelt __instance;
                public XUiController[] ___itemControllers;
                public int ___currentHoldingIndex;
            }

            public static void Postfix(XUiC_Toolbelt __instance, XUiController[] ___itemControllers, int ___currentHoldingIndex)
            {
                if (Input.GetKey(fastRepairHotKey))
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
