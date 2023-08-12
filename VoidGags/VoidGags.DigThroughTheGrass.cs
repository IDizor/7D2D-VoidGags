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
        public void ApplyPatches_DigThroughTheGrass(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ItemActionDynamic), "hitTarget"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemActionDynamic_hitTarget.APostfix p) =>
                ItemActionDynamic_hitTarget.Postfix(p.__instance, p._actionData, p.hitInfo))));

            LogPatchApplied(nameof(Settings.DigThroughTheGrass));
        }

        /// <summary>
        /// Dig with a shovel or pickaxe through the grass/plants.
        /// </summary>
        public class ItemActionDynamic_hitTarget
        {
            public static FastTags AxeTag = FastTags.Parse("axe");
            public static FastTags ShovelTag = FastTags.Parse("shovel");
            public static FastTags MiningToolTag = FastTags.Parse("miningTool");

            public struct APostfix
            {
                public ItemActionDynamic __instance;
                public ItemActionData _actionData;
                public WorldRayHitInfo hitInfo;
            }

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
