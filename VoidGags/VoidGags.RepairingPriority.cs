using HarmonyLib;
using static VoidGags.VoidGags.RepairingHasTopPriority;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_RepairingPriority()
        {
            LogApplyingPatch(nameof(Settings.RepairingHasTopPriority));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_CraftingQueue), nameof(XUiC_CraftingQueue.AddItemToRepair)),
                postfix: new HarmonyMethod(XUiC_CraftingQueue_AddItemToRepair.Postfix));
        }

        public static class RepairingHasTopPriority
        {
            /// <summary>
            /// New items to repair are always placed to the top of the crafting queue.
            /// </summary>
            public static class XUiC_CraftingQueue_AddItemToRepair
            {
                public static void Postfix(XUiC_CraftingQueue __instance, ref bool __result)
                {
                    if (__result)
                    {
                        if (__instance.queueItems.Length > 1)
                        {
                            __instance.HaltCrafting();
                            XUiC_RecipeStack repairingItem = null;

                            for (int i = 0; i < __instance.queueItems.Length - 1; i++)
                            {
                                if (__instance.queueItems[i] is XUiC_RecipeStack recipeStack && recipeStack.HasRecipe())
                                {
                                    if (repairingItem == null)
                                    {
                                        repairingItem = new XUiC_RecipeStack();
                                        recipeStack.CopyTo(repairingItem);
                                    }

                                    if (repairingItem != null)
                                    {
                                        ((XUiC_RecipeStack)__instance.queueItems[i + 1]).CopyTo(recipeStack);
                                    }
                                }
                            }

                            if (repairingItem != null)
                            {
                                repairingItem.CopyTo((XUiC_RecipeStack)__instance.queueItems[__instance.queueItems.Length - 1]);
                            }

                            __instance.ResumeCrafting();
                        }
                    }
                }
            }
        }
    }
}
