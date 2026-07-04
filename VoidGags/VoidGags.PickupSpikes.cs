using System;
using System.Collections.Generic;
using Audio;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using static VoidGags.VoidGags.PickupSpikes;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PickupSpikes()
        {
            LogApplyingPatch(nameof(Settings.PickupSpikes));

            Harmony.Patch(AccessTools.Method(typeof(GameUtils), nameof(GameUtils.HarvestOnAttack)),
                prefix: new HarmonyMethod(GameUtils_HarvestOnAttack.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(Block), nameof(Block.OnBlockDamaged)),
                prefix: new HarmonyMethod(Block_OnBlockDamaged.Prefix),
                postfix: new HarmonyMethod(Block_OnBlockDamaged.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(ItemActionRepair), nameof(ItemActionRepair.ExecuteAction)),
                prefix: new HarmonyMethod(ItemActionRepair_ExecuteAction.Prefix));

            UseXmlPatches(nameof(Settings.PickupSpikes));
        }

        public static class PickupSpikes
        {
            public const string MaterialTrapIron = "MtrapSpikesIron";
            public const string MaterialTrapFence = "MbarbedFence";

            public static FastTags<TagGroup.Global> MeleeTag = FastTags<TagGroup.Global>.Parse("melee");
            public static FastTags<TagGroup.Global> SalvageToolTag = FastTags<TagGroup.Global>.Parse("salvageTool");

            public static List<Vector3i> NonPickableTraps = [];
            public static Dictionary<Vector3i, int> PickableTraps = [];

            public static bool IsSpikesTrap(Block block)
            {
                return block.BlockTag == BlockTags.Spike &&
                    block.FilterTags.Any(tag => tag.Same("SC_traps"));
            }

            public static bool IsIronTrapMaterial(string material)
            {
                return material.Same(MaterialTrapIron) || material.Same(MaterialTrapFence);
            }

            public static void AddNonPickableTrap(Vector3i trapPos)
            {
                if (NonPickableTraps.Contains(trapPos)) return;
                NonPickableTraps.Add(trapPos);
            }

            public static void AddPickableTrap(Vector3i trapPos, int blockType)
            {
                if (PickableTraps.ContainsKey(trapPos)) return;
                PickableTraps.Add(trapPos, blockType);
            }

            /// <summary>
            /// Pickup iron traps with salvage tools.
            /// </summary>
            public static class GameUtils_HarvestOnAttack
            {
                public static bool Prefix(ItemActionData _actionData, Dictionary<string, ItemActionAttack.Bonuses> ToolBonuses)
                {
                    var block = _actionData.attackDetails.blockBeingDamaged.Block;

                    if (IsSpikesTrap(block) &&
                        _actionData.attackDetails.WeaponTypeTag.Test_AnySet(MeleeTag) &&
                        !NonPickableTraps.Contains(_actionData.attackDetails.raycastHitPosition) &&
                        _actionData.invData.holdingEntity is EntityPlayerLocal player)
                    {
                        var material = block.blockMaterial.id;
                        if (IsIronTrapMaterial(material))
                        {
                            var blockPos = _actionData.attackDetails.raycastHitPosition;
                            var weaponTags = player.inventory.holdingItem.ItemTags;
                            var isSalvageTool = weaponTags.Test_AnySet(SalvageToolTag);
                            if (isSalvageTool)
                            {
                                AddPickableTrap(blockPos, block.blockID);
                                if (_actionData.attackDetails.bKilled)
                                {
                                    if (block.DowngradeBlock.isair && PickableTraps.ContainsKey(blockPos))
                                    {
                                        player.GiveItem(new(new(PickableTraps[blockPos]), 1));
                                        PickableTraps.Remove(blockPos);
                                    }
                                }
                                return false;
                            }
                            else
                            {
                                PickableTraps.Remove(blockPos);
                                return true;
                            }
                        }
                    }

                    return true;
                }
            }

            /// <summary>
            /// Track POI traps to make them non-pickable.
            /// </summary>
            public static class Block_OnBlockDamaged
            {
                public static void Prefix(BlockValueRef _bvRef, BlockValue _blockValue)
                {
                    if (IsSpikesTrap(_blockValue.Block) &&
                        _blockValue.Block.blockName.EndsWith("POI", StringComparison.OrdinalIgnoreCase))
                    {
                        AddNonPickableTrap(_bvRef.BlockPosition);
                    }
                }

                public static void Postfix(WorldBase _world, BlockValueRef _bvRef, BlockValue _blockValue, int __result)
                {
                    if (IsSpikesTrap(_blockValue.Block))
                    {
                        if (__result >= _blockValue.Block.MaxDamage)
                        {
                            if (_world.GetBlock(_bvRef.BlockPosition).isair)
                            {
                                NonPickableTraps.Remove(_bvRef.BlockPosition);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Disallow to repair POI traps.
            /// </summary>
            public static class ItemActionRepair_ExecuteAction
            {
                public static bool Prefix(ItemActionRepair __instance, ItemActionData _actionData, bool _bReleased)
                {
                    if (!_bReleased)
                    {
                        if (Time.time - _actionData.lastUseTime < __instance.Delay)
                        {
                            return false;
                        }
                        var player = _actionData.invData.holdingEntity as EntityPlayerLocal;
                        var hitInfo = player?.HitInfo;
                        if (hitInfo != null)
                        {
                            if (!hitInfo.bHitValid || Helper.IsInvulnerableBlock(hitInfo.hit.blockPos) || !GameUtils.IsBlockOrTerrain(hitInfo.tag))
                            {
                                return false;
                            }

                            var block = player.world.GetBlock(hitInfo.hit.blockPos);
                            if (IsSpikesTrap(block.Block) &&
                                (NonPickableTraps.Contains(hitInfo.hit.blockPos) || block.Block.blockName.EndsWith("POI", StringComparison.OrdinalIgnoreCase)))
                            {
                                Manager.PlayInsidePlayerHead("keystone_build_warning");
                                return false;
                            }
                        }
                    }
                    return true;
                }
            }
        }
    }
}
