using HarmonyLib;
using static VoidGags.VoidGags.ScrapTimeAndSalvageOperations;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ScrapTime()
        {
            LogApplyingPatch(nameof(Settings.ScrapTimeAndSalvageOperations));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_CraftingWindowGroup), nameof(XUiC_CraftingWindowGroup.AddItemToQueue), [typeof(Recipe), typeof(int)]),
                prefix: new HarmonyMethod(XUiC_CraftingWindowGroup_AddItemToQueue.Prefix, Priority.Low));
        }

        public static class ScrapTimeAndSalvageOperations
        {
            /// <summary>
            /// Makes the scrapping process in inventory faster, depending on the Salvage Operations perk level.
            /// </summary>
            public static class XUiC_CraftingWindowGroup_AddItemToQueue
            {
                public static void Prefix(Recipe _recipe)
                {
                    if (Helper.GetCallerMethod().DeclaringType == typeof(ItemActionEntryScrap))
                    {
                        var salvageOperations = Helper.PlayerLocal.PerkSalvageOperations();
                        _recipe.craftingTime *= 1f - (0.8f * salvageOperations.Level / salvageOperations.ProgressionClass.MaxLevel);
                    }
                }
            }
        }
    }
}
