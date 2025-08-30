using System.Collections.Generic;
using HarmonyLib;
using UniLinq;
using UnityEngine;
using static VoidGags.VoidGags.ExplosionMining;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ExplosionMining()
        {
            LogApplyingPatch(nameof(Settings.ExplosionMining));

            Harmony.Patch(AccessTools.Method(typeof(Block), nameof(Block.OnBlockDestroyedByExplosion)), null,
                postfix: new HarmonyMethod(Block_OnBlockDestroyedByExplosion.Postfix));
        }

        public static class ExplosionMining
        {
            /// <summary>
            /// Terrain explosions allow to collect terrain resources (soil, sand, ores, etc).
            /// </summary>
            public static class Block_OnBlockDestroyedByExplosion
            {
                public static void Postfix(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue)
                {
                    if (_blockValue.Block.shape.IsTerrain())
                    {
                        if (GameManager.Instance != null && _blockValue.Block.itemsToDrop.TryGetValue(EnumDropEvent.Harvest, out List<Block.SItemDropProb> drop) && drop.Count > 0)
                        {
                            // wait for a while and spawn resources
                            var random = _world.GetGameRandom();
                            Helper.DeferredAction(0.1f + random.RandomFloat / 5, () =>
                            {
                                var chunk = (Chunk)GameManager.Instance.World.GetChunkSync(World.toChunkXZ(_blockPos.x), World.toChunkXZ(_blockPos.z));
                                var dropPos = FindAirBlockForDrop(_world, _blockPos);
                                var nearItems = chunk.GetEntities<EntityItem>(dropPos, 3);

                                foreach (var item in drop)
                                {
                                    // only harvestable common resources
                                    if (!item.tag.Same("oreWoodHarvest")) continue;

                                    // truncate resources to drop, except ores
                                    var lootCount = item.name.StartsWith("terrOre") ? item.maxCount : (int)(item.maxCount * 0.6666f);
                                    if (lootCount > 0)
                                    {
                                        var itemType = ItemClass.GetItem(item.name).type;
                                        var nearItem = nearItems.FirstOrDefault(i => i.itemStack != null && i.itemStack.itemValue.type == itemType);

                                        // add items to the same near EntityItem
                                        if (nearItem != null)
                                        {
                                            nearItem.itemStack.count += lootCount;
                                        }
                                        else
                                        {
                                            GameManager.Instance.ItemDropServer(new ItemStack(new ItemValue(itemType), lootCount), dropPos, Vector3.zero);
                                        }
                                    }
                                }
                            });
                        }
                    }

                    static Vector3 FindAirBlockForDrop(WorldBase world, Vector3i pos)
                    {
                        for (var i = 1; i <= 5; i++)
                        {
                            for (var x = -i; x <= i; x++)
                            {
                                for (var z = -i; z <= i; z++)
                                {
                                    for (var y = 0; y <= i; y++)
                                    {
                                        var p = new Vector3i(pos.x + x, pos.y + y, pos.z + z);
                                        if (world.GetBlock(p).isair)
                                        {
                                            return p.ToVector3Center();
                                        }
                                    }
                                }
                            }
                        }

                        return pos.ToVector3Center();
                    }
                }
            }
        }
    }
}
