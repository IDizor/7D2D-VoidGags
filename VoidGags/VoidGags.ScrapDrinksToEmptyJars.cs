using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ScrapDrinksToEmptyJars(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(XUiC_CraftingWindowGroup), "AddItemToQueue", new Type[] { typeof(Recipe), typeof(int) }),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((Recipe _recipe) => XUiC_CraftingWindowGroup_AddItemToQueue_2.Prefix(_recipe)), Priority.Normal));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.ScrapDrinksToEmptyJars)}");
        }

        /// <summary>
        /// Scrap any drink like water/murky/tea/coffee/etc to an empty jar.
        /// </summary>
        public class XUiC_CraftingWindowGroup_AddItemToQueue_2
        {
            private static string[] glassVesselPrefixes = new string[] { "drinkJar", "drinkYuccaJuice", "foodHoney" };
            private static string[] plasticVesselPrefixes = new string[] { "ulmDrinkPlasticBottle" };
            private static string[] skipVessels = new string[] { "drinkJarEmpty", "ulmDrinkPlasticBottleEmpty" };

            public static void Prefix(Recipe _recipe)
            {
                if (Helper.GetCallerMethod().DeclaringType == typeof(ItemActionEntryScrap))
                {
                    if (_recipe.ingredients.Count == 1)
                    {
                        var itemValue = _recipe.ingredients[0].itemValue;
                        if (itemValue.ItemClass != null && !skipVessels.Any(v => v.Same(itemValue.ItemClass.Name)))
                        {
                            var isJar = glassVesselPrefixes.Any(p => itemValue.ItemClass.Name.StartsWith(p));
                            var isBottle = !isJar && plasticVesselPrefixes.Any(p => itemValue.ItemClass.Name.StartsWith(p));
                            if (isJar || isBottle)
                            {
                                var drinkEmpty = isJar
                                    ? ItemClass.GetItemClass("drinkJarEmpty")
                                    : ItemClass.GetItemClass("ulmDrinkPlasticBottleEmpty");

                                if (drinkEmpty != null)
                                {
                                    _recipe.itemValueType = drinkEmpty.Id;
                                    _recipe.count = _recipe.ingredients[0].count;
                                    _recipe.craftingTime = _recipe.count * (isJar ? 2f : 4f);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
