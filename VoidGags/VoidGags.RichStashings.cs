using System;
using HarmonyLib;
using static VoidGags.VoidGags.RichStashings;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_RichStashings()
        {
            LogApplyingPatch(nameof(Settings.RichStashings));

            Harmony.Patch(AccessTools.Method(typeof(LootManager), nameof(LootManager.LootContainerOpened)),
                prefix: new HarmonyMethod(LootManager_LootContainerOpened.Prefix),
                postfix: new HarmonyMethod(LootManager_LootContainerOpened.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(LootContainer), nameof(LootContainer.SpawnLootItemsFromList)),
                prefix: new HarmonyMethod(LootContainer_SpawnLootItemsFromList.Prefix));
        }

        public static class RichStashings
        {
            public static bool IsHiddenStashing = false;
            public static Vector3i[] Offsets =
            [
                Vector3i.up,
                Vector3i.down,
                Vector3i.left,
                Vector3i.right,
                Vector3i.forward,
                Vector3i.back,
            ];

            /// <summary>
            /// Check if opened container is hidden stashing.
            /// </summary>
            public static class LootManager_LootContainerOpened
            {
                public static void Prefix(ITileEntityLootable _tileEntity)
                {
                    if (_tileEntity.GetChunk() != null)
                    {
                        var containerName = _tileEntity.blockValue.Block.blockName;
                        if (containerName.StartsWith("cntSportsBag", StringComparison.OrdinalIgnoreCase) ||
                            containerName.StartsWith("cntTrashPile", StringComparison.OrdinalIgnoreCase) ||
                            containerName.StartsWith("cntPurse", StringComparison.OrdinalIgnoreCase) ||
                            containerName.StartsWith("cntDuffle", StringComparison.OrdinalIgnoreCase) ||
                            containerName.StartsWith("cntBackpack", StringComparison.OrdinalIgnoreCase) ||
                            containerName.StartsWith("cntFoodPileSmall", StringComparison.OrdinalIgnoreCase) ||
                            containerName.StartsWith("cntAmmoPileSmall", StringComparison.OrdinalIgnoreCase) ||
                            containerName.StartsWith("cntWeaponsBagSmall", StringComparison.OrdinalIgnoreCase))
                        {
                            var blockPos = _tileEntity.ToWorldPos();
                            if (blockPos.IsValid && blockPos != Vector3i.zero)
                            {
                                var world = GameManager.Instance.World;
                                var surroundings = 0;
                                var weakSides = 0;
                                foreach (var offset in Offsets)
                                {
                                    var block = world.GetBlock(blockPos + offset);
                                    if (!block.isair && !block.isWater && !block.Block.IsTerrainDecoration)
                                    {
                                        var blockName = block.Block.blockName;
                                        if ((blockName.StartsWith("terr", StringComparison.OrdinalIgnoreCase) || blockName.Contains("Shapes:")) &&
                                            !blockName.StartsWith("frameShapes", StringComparison.OrdinalIgnoreCase))
                                        {
                                            surroundings++;
                                        }
                                        else if (offset.y == 0 && block.Block.MaxDamage <= 50)
                                        {
                                            weakSides++;
                                        }
                                    }
                                }
                                if (weakSides == 1) surroundings++;
                                if (surroundings > 4)
                                {
                                    IsHiddenStashing = true;
                                    //LogInfo("Hidden stashing: " + containerName);
                                }
                            }
                        }
                    }
                }

                public static void Postfix()
                {
                    IsHiddenStashing = false;
                }
            }

            /// <summary>
            /// Apply loot bonus for hidden stashings.
            /// </summary>
            public static class LootContainer_SpawnLootItemsFromList
            {
                public static void Prefix(ref float abundance, ref float rareLootChance)
                {
                    if (IsHiddenStashing)
                    {
                        IsHiddenStashing = false;
                        abundance *= 5f;
                        rareLootChance = (rareLootChance + 0.1f) * 5f;
                    }
                }
            }
        }
    }
}
