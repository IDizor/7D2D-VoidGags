using HarmonyLib;
using static VoidGags.VoidGags.CraftingQueueMove;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_CraftingQueueMove()
        {
            LogApplyingPatch(nameof(Settings.CraftingQueueRightClickToMove));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_RecipeStack), nameof(XUiC_RecipeStack.Init)),
                postfix: new HarmonyMethod(XUiC_RecipeStack_Init.Postfix));
        }

        public static class CraftingQueueMove
        {
            /// <summary>
            /// Moves right-clicked item to the top of the crafting queue.
            /// </summary>
            public static class XUiC_RecipeStack_Init
            {
                public static void Postfix(XUiC_RecipeStack __instance, XUiController ___background)
                {
                    if (___background != null)
                    {
                        ___background.OnRightPress += new XUiEvent_OnPressEventHandler((controller, _) =>
                        {
                            if (__instance.GetRecipe() != null)
                            {
                                var craftingQueue = __instance.Owner.queueItems;
                                XUiC_RecipeStack tempItem = null;

                                if (craftingQueue != null && craftingQueue.Length > 1)
                                {
                                    __instance.Owner.HaltCrafting();

                                    for (int i = 0; i < craftingQueue.Length - 1; i++)
                                    {
                                        if (craftingQueue[i] is XUiC_RecipeStack recipeStack && recipeStack.HasRecipe())
                                        {
                                            if (__instance == recipeStack)
                                            {
                                                tempItem = new XUiC_RecipeStack();
                                                recipeStack.CopyTo(tempItem);
                                            }

                                            if (tempItem != null)
                                            {
                                                ((XUiC_RecipeStack)craftingQueue[i + 1]).CopyTo(recipeStack);
                                            }
                                        }
                                    }

                                    if (tempItem != null)
                                    {
                                        tempItem.CopyTo((XUiC_RecipeStack)craftingQueue[craftingQueue.Length - 1]);
                                    }

                                    __instance.Owner.ResumeCrafting();
                                }
                            }
                        });
                    }
                }
            }
        }
    }
}
