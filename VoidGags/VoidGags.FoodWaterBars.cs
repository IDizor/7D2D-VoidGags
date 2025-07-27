using HarmonyLib;

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

            Harmony.Patch(AccessTools.Method(typeof(XUiC_HUDStatBar), nameof(XUiC_HUDStatBar.GetBindingValue)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_HUDStatBar_GetBindingValue.APrefix p) => XUiC_HUDStatBar_GetBindingValue.Prefix(p.__instance, ref p.value, p.bindingName, ref p.__result))));
        }

        /// <summary>
        /// Binding values for food and water.
        /// </summary>
        public class XUiC_HUDStatBar_GetBindingValue
        {
            public struct APrefix
            {
                public XUiC_HUDStatBar __instance;
                public string value;
                public string bindingName;
                public bool __result;
            }

            public static bool Prefix(XUiC_HUDStatBar __instance, ref string value, string bindingName, ref bool __result)
            {
                var player = __instance.LocalPlayer;

                switch (bindingName)
                {
                    case "playermodifiedcurrentwater":
                        value = player == null ? "" : XUiM_Player.GetModifiedCurrentWater(player).ToString("0");
                        __result = true;
                        return false;
                    case "playerwatermax":
                        value = player == null ? "" : XUiM_Player.GetWaterMax(player).ToString("0");
                        __result = true;
                        return false;
                    case "playermodifiedcurrentfood":
                        value = player == null ? "" : XUiM_Player.GetModifiedCurrentFood(player).ToString("0");
                        __result = true;
                        return false;
                    case "playerfoodmax":
                        value = player == null ? "" : XUiM_Player.GetFoodMax(player).ToString("0");
                        __result = true;
                        return false;
                }

                return true;
            }
        }
    }
}
