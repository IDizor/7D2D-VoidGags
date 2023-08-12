using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_RepairPriority(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(XUiC_CraftingQueue), "AddItemToRepair"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_CraftingQueue_AddItemToRepair.APostfix p) =>
                XUiC_CraftingQueue_AddItemToRepair.Postfix(p.__instance, ref p.___queueItems, ref p.__result))));

            LogPatchApplied(nameof(Settings.RepairHasTopPriority));
        }

        /// <summary>
        /// New items to repair are always placed to the top of the crafting queue.
        /// </summary>
        public class XUiC_CraftingQueue_AddItemToRepair
        {
            public struct APostfix
            {
                public XUiC_CraftingQueue __instance;
                public XUiController[] ___queueItems;
                public bool __result;
            }

            public static void Postfix(XUiC_CraftingQueue __instance, ref XUiController[] ___queueItems, ref bool __result)
            {
                if (__result)
                {
                    if (___queueItems.Length > 1)
                    {
                        __instance.HaltCrafting();
                        XUiC_RecipeStack repairingItem = null;

                        for (int i = 0; i < ___queueItems.Length - 1; i++)
                        {
                            if (___queueItems[i] is XUiC_RecipeStack recipeStack && recipeStack.HasRecipe())
                            {
                                if (repairingItem == null)
                                {
                                    repairingItem = new XUiC_RecipeStack();
                                    recipeStack.CopyTo(repairingItem);
                                }

                                if (repairingItem != null)
                                {
                                    ((XUiC_RecipeStack)___queueItems[i + 1]).CopyTo(recipeStack);
                                }
                            }
                        }

                        if (repairingItem != null)
                        {
                            repairingItem.CopyTo((XUiC_RecipeStack)___queueItems[___queueItems.Length - 1]);
                        }

                        __instance.ResumeCrafting();
                    }
                }
            }
        }
    }
}
