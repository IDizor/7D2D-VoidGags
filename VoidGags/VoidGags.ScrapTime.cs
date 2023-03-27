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
        public void ApplyPatches_ScrapTime(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(XUiC_CraftingWindowGroup), "AddItemToQueue", new Type[] { typeof(Recipe), typeof(int) }),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((Recipe _recipe) => XUiC_CraftingWindowGroup_AddItemToQueue.Prefix(_recipe)), Priority.Low));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.ScrapTimeAndSalvageOperations)}");
        }

        /// <summary>
        /// Makes the scrapping process in inventory faster, depending on the Salvage Operations perk level.
        /// </summary>
        public class XUiC_CraftingWindowGroup_AddItemToQueue
        {
            public static void Prefix(Recipe _recipe)
            {
                if (Helper.GetCallerMethod().DeclaringType == typeof(ItemActionEntryScrap))
                {
                    var salvageOperations = Helper.PlayerLocal.Progression.GetProgressionValue("perkSalvageOperations");
                    _recipe.craftingTime *= 1f - (0.6666f * salvageOperations.Level / salvageOperations.ProgressionClass.MaxLevel);
                }
            }
        }
    }
}
