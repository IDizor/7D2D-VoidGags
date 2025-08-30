using HarmonyLib;
using static VoidGags.VoidGags.FoodWaterBars;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_FoodWaterBars()
        {
            LogApplyingPatch(nameof(Settings.FoodWaterBars));
            UseXmlPatches(nameof(Settings.FoodWaterBars));

            Harmony.Patch(AccessTools.Method(typeof(XUiController), nameof(XUiController.GetBindingValue)),
                prefix: new HarmonyMethod(XUiController_GetBindingValue.Prefix));
        }

        public static class FoodWaterBars
        {
            /// <summary>
            /// Binding values for food and water.
            /// </summary>
            public static class XUiController_GetBindingValue
            {
                public static bool Prefix(XUiController __instance, ref string _value, string _bindingName, ref bool __result)
                {
                    if (__instance is XUiC_HUDStatBar hudStatBar)
                    {
                        var player = hudStatBar.LocalPlayer;
                        switch (_bindingName)
                        {
                            case "playermodifiedcurrentwater":
                                _value = player == null ? "" : XUiM_Player.GetModifiedCurrentWater(player).ToString("0");
                                __result = true;
                                return false;
                            case "playerwatermax":
                                _value = player == null ? "" : XUiM_Player.GetWaterMax(player).ToString("0");
                                __result = true;
                                return false;
                            case "playermodifiedcurrentfood":
                                _value = player == null ? "" : XUiM_Player.GetModifiedCurrentFood(player).ToString("0");
                                __result = true;
                                return false;
                            case "playerfoodmax":
                                _value = player == null ? "" : XUiM_Player.GetFoodMax(player).ToString("0");
                                __result = true;
                                return false;
                        }
                    }
                    return true;
                }
            }
        }
    }
}
