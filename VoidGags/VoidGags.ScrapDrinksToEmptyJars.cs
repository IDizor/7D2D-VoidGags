using System.Linq;
using HarmonyLib;
using VoidGags.Types;
using static VoidGags.VoidGags.ScrapDrinksToEmptyJars;
using static XUiC_ItemActionList;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ScrapDrinksToEmptyJars()
        {
            LogApplyingPatch(nameof(Settings.ScrapDrinksToEmptyJars));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ItemActionList), nameof(XUiC_ItemActionList.SetCraftingActionList)),
                postfix: new HarmonyMethod(XUiC_ItemActionList_SetCraftingActionList.Postfix));
        }

        public static class ScrapDrinksToEmptyJars
        {
            /// <summary>
            /// Scrap any drink like water/murky/tea/coffee/etc to an empty jar.
            /// </summary>
            public static class XUiC_ItemActionList_SetCraftingActionList
            {
                public static void Postfix(XUiC_ItemActionList __instance, ItemActionListTypes _actionListType, XUiController itemController)
                {
                    if (_actionListType == ItemActionListTypes.Item &&
                        itemController is XUiC_ItemStack stack &&
                        __instance.itemActionEntries.All(a => a is not ItemActionEntryScrap))
                    {
                        var eatAction = stack.itemClass.Actions.FirstOrDefault(a => a is ItemActionEat) as ItemActionEat;
                        if (eatAction != null && eatAction.UseJarRefund)
                        {
                            __instance.AddActionListEntry(new ItemActionEntryScrapDrink(itemController));
                        }
                    }
                }
            }
        }
    }
}
