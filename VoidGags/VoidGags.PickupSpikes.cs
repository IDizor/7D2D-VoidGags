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

            Harmony.Patch(AccessTools.Method(typeof(World), nameof(World.CanPickupBlockAt)),
                prefix: new HarmonyMethod(World_CanPickupBlockAt.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(WorldBase), nameof(WorldBase.SetBlockRPC), [typeof(BlockValueRef), typeof(BlockValue)]),
                prefix: new HarmonyMethod(WorldBase_SetBlockRPC.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(Chunk), nameof(Chunk.SetBlock)),
                prefix: new HarmonyMethod(Chunk_SetBlock.Prefix));

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
            public static List<Vector3i> AllowedPoiTraps = [];

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

            public static BlockValue GetPrefabBlock(PrefabInstance poi, Vector3i blockPos)
            {
                if (poi == null) return BlockValue.Air;
                var posInPrefab = blockPos - poi.boundingBoxPosition; // vanilla method GetPositionRelativeToPoi() has issues
                return poi.prefab.GetBlock(posInPrefab.x, posInPrefab.y, posInPrefab.z);
            }

            public static bool IsPrefabBlock(EntityPlayer player, BlockValue block, Vector3i blockPos)
            {
                return Helper.DowngradableBlocksEqual(block, GetPrefabBlock(player.prefab, blockPos));
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
            /// Track POI traps to make them non-pickable if repaired somehow.
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
            /// Disallow to pickup POI traps.
            /// </summary>
            public static class World_CanPickupBlockAt
            {
                public static bool Prefix(Vector3i blockPos, ref bool __result)
                {
                    if (IsDedicatedServer || AllowedPoiTraps.Contains(blockPos)) return true;

                    if (NonPickableTraps.Contains(blockPos))
                    {
                        __result = false;
                        return false;
                    }
                    else
                    {
                        var player = Helper.PlayerLocal;
                        var poi = player?.prefab; // or player.world.GetPOIAtPosition(player.position) can be used
                        if (poi != null)
                        {
                            var block = player.world.GetBlock(blockPos);
                            if (IsSpikesTrap(block.Block) && IsPrefabBlock(player, block, blockPos))
                            {
                                AddNonPickableTrap(blockPos);
                                __result = false;
                                return false;
                            }
                        }
                    }
                    return true;
                }
            }

            /// <summary>
            /// Add destroyed POI traps to AllowedPoiTraps.
            /// </summary>
            public static class WorldBase_SetBlockRPC
            {
                public static void Prefix(BlockValueRef _bvRef, BlockValue _blockValue)
                {
                    if (IsDedicatedServer) return;

                    if (_blockValue.isair)
                    {
                        var world = GameManager.Instance.World;
                        var destroyedBlock = world.GetBlock(_bvRef.BlockPosition);
                        if (!destroyedBlock.isair && IsSpikesTrap(destroyedBlock.Block) && !AllowedPoiTraps.Contains(_bvRef.BlockPosition))
                        {
                            var poi = world.GetPOIAtPosition(_bvRef.BlockPosition);
                            if (poi != null)
                            {
                                var prefabBlock = GetPrefabBlock(poi, _bvRef.BlockPosition);
                                if (Helper.DowngradableBlocksEqual(destroyedBlock, prefabBlock))
                                {
                                    //LogWarning($"Allowed POI trap added: {prefabBlock.Block.blockName}");
                                    AllowedPoiTraps.Add(_bvRef.BlockPosition);
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// On POI reset clear AllowedPoiTraps.
            /// </summary>
            public static class Chunk_SetBlock
            {
                public static void Prefix(int ___m_X, int ___m_Z, int x, int y, int z, BlockValue _blockValue)
                {
                    if (IsDedicatedServer) return;

                    if (IsSpikesTrap(_blockValue.Block))
                    {
                        var trapPos = new Vector3i((___m_X << 4) + x, y, (___m_Z << 4) + z);
                        if (AllowedPoiTraps.Contains(trapPos))
                        {
                            // if called from POI reset code: Prefab.CopyBlocksIntoChunkNoEntities() <- PrefabInstance.CopyIntoChunk()
                            var caller = Helper.GetCallerMethod();
                            if (caller.DeclaringType == typeof(Prefab) && caller.Name == nameof(Prefab.CopyBlocksIntoChunkNoEntities))
                            {
                                AllowedPoiTraps.Remove(trapPos);
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
                            if (IsSpikesTrap(block.Block) && !AllowedPoiTraps.Contains(hitInfo.hit.blockPos))
                            {
                                if (NonPickableTraps.Contains(hitInfo.hit.blockPos) ||
                                    block.Block.blockName.EndsWith("POI", StringComparison.OrdinalIgnoreCase) ||
                                    IsPrefabBlock(player, block, hitInfo.hit.blockPos))
                                {
                                    Manager.PlayInsidePlayerHead("keystone_build_warning");
                                    return false;
                                }
                            }
                        }
                    }
                    return true;
                }
            }
        }
    }
}
