using System.Linq;
using HarmonyLib;
using static VoidGags.VoidGags.DigThroughTheGrass;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_DigThroughTheGrass()
        {
            LogApplyingPatch(nameof(Settings.DigThroughTheGrass));

            Harmony.Patch(AccessTools.Method(typeof(ItemActionDynamic), nameof(ItemActionDynamic.hitTarget)),
                postfix: new HarmonyMethod(ItemActionDynamic_hitTarget.Postfix));
        }

        public static class DigThroughTheGrass
        {
            public static FastTags<TagGroup.Global> AxeTag = FastTags<TagGroup.Global>.Parse("axe");
            public static FastTags<TagGroup.Global> ShovelTag = FastTags<TagGroup.Global>.Parse("shovel");
            public static FastTags<TagGroup.Global> MiningToolTag = FastTags<TagGroup.Global>.Parse("miningTool");

            /// <summary>
            /// Dig with a shovel or pickaxe through the grass/plants.
            /// </summary>
            public static class ItemActionDynamic_hitTarget
            {
                public static void Postfix(ItemActionDynamic __instance, ItemActionData _actionData, WorldRayHitInfo hitInfo)
                {
                    if (_actionData.invData.itemValue?.ItemClass != null)
                    {
                        var block = hitInfo.hit.blockValue.Block;
                        if (__instance is ItemActionDynamicMelee meleeAction && block.IsTerrainDecoration && block.MaxDamage < 4)
                        {
                            var itemTags = _actionData.invData.itemValue.ItemClass.ItemTags;
                            if ((itemTags.Test_AnySet(ShovelTag) || itemTags.Test_AnySet(MiningToolTag)) && !itemTags.Test_AnySet(AxeTag))
                            {
                                var isCrop = false;
                                if (block.FilterTags != null &&
                                    block.FilterTags.Any(tag => tag.Same("SC_crops")))
                                {
                                    if (block.GetBlockName().ToLower().Contains("mushroom"))
                                    {
                                        isCrop = true;
                                    }
                                    else
                                    {
                                        var surfaceBlock = GameManager.Instance.World.GetBlock(hitInfo.hit.blockPos + new Vector3i(0, -1, 0));
                                        isCrop = surfaceBlock.Block.GetBlockName().StartsWith("farmPlot");
                                    }
                                }
                                if (!isCrop)
                                {
                                    var itemActionDynamicData = (ItemActionDynamicMelee.ItemActionDynamicMeleeData)_actionData;
                                    itemActionDynamicData.StaminaUsage = 0;
                                    meleeAction.Raycast(itemActionDynamicData);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
