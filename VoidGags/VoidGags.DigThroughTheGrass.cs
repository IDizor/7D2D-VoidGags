using System.Linq;
using HarmonyLib;
using UnityEngine;
using static ItemActionDynamic;

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
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemActionDynamic_hitTarget_Params p) =>
                ItemActionDynamic_hitTarget.Postfix(p.__instance, p._actionData, p.hitInfo))));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.DigThroughTheGrass)}");
        }

        private struct ItemActionDynamic_hitTarget_Params
        {
            public ItemActionDynamic __instance;
            public ItemActionData _actionData;
            public WorldRayHitInfo hitInfo;
        }

        /// <summary>
        /// Dig with a shovel or pickaxe through the grass/plants.
        /// </summary>
        public class ItemActionDynamic_hitTarget
        {
            public static FastTags ShovelTag = FastTags.Parse("shovel");
            public static FastTags MiningToolTag = FastTags.Parse("miningTool");

            public static void Postfix(ItemActionDynamic __instance, ItemActionData _actionData, WorldRayHitInfo hitInfo)
            {
                var block = hitInfo.hit.blockValue.Block;
                if (__instance is ItemActionDynamicMelee meleeAction && block.IsTerrainDecoration && block.MaxDamage < 4)
                {
                    var itemTags = _actionData.invData.itemValue.ItemClass.ItemTags;
                    if (itemTags.Test_AnySet(ShovelTag) || itemTags.Test_AnySet(MiningToolTag))
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
                            meleeAction.Raycast((ItemActionDynamicData)_actionData);
                        }
                    }
                }
            }
        }
    }
}
