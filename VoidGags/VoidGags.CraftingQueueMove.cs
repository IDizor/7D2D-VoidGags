using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_CraftingQueueMove(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(XUiC_RecipeStack), "Init"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_RecipeStack_Init.APostfix p) =>
                XUiC_RecipeStack_Init.Postfix(p.__instance, p.___background))));

            LogPatchApplied(nameof(Settings.CraftingQueueRightClickToMove));
        }

        /// <summary>
        /// Moves right-clicked item to the top of the crafting queue.
        /// </summary>
        public class XUiC_RecipeStack_Init
        {
            public struct APostfix
            {
                public XUiC_RecipeStack __instance;
                public XUiController ___background;
            }

            public static void Postfix(XUiC_RecipeStack __instance, XUiController ___background)
            {
                if (___background != null)
                {
                    ___background.OnRightPress += new XUiEvent_OnPressEventHandler((controller, _) => {
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
