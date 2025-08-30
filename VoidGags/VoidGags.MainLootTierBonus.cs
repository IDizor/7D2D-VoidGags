using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.MainLootTierBonus;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_MainLootTierBonus()
        {
            LogApplyingPatch(nameof(Settings.MainLootTierBonus));

            Harmony.Patch(AccessTools.Method(typeof(LootContainer), nameof(LootContainer.SpawnLootItemsFromList)),
                prefix: new HarmonyMethod(LootContainer_SpawnLootItemsFromList.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(LootManager), nameof(LootManager.LootContainerOpened)),
                prefix: new HarmonyMethod(LootManager_LootContainerOpened.Prefix),
                postfix: new HarmonyMethod(LootManager_LootContainerOpened.Postfix));
        }

        public static class MainLootTierBonus
        {
            public static FastTags<TagGroup.Global> Safes = FastTags<TagGroup.Global>.Parse("safes");
            public static bool ApplyBonus = false;
            public static int PoiTier = 0;

            /// <summary>
            /// Applies better tier loot bonus for the main loot containers.
            /// </summary>
            public static class LootContainer_SpawnLootItemsFromList
            {
                public static void Prefix(ref float abundance)
                {
                    if (ApplyBonus)
                    {
                        ApplyBonus = false;
                        if (PoiTier > 0)
                        {
                            abundance *= 1.2f + (0.2f * (PoiTier - 1));
                        }
                    }
                }
            }

            /// <summary>
            /// Checks if opened container can be considered as a main loot container and allows to apply bonus to it.
            /// </summary>
            public static class LootManager_LootContainerOpened
            {
                public static void Prefix(ITileEntityLootable _tileEntity, int _entityIdThatOpenedIt, FastTags<TagGroup.Global> _containerTags)
                {
                    if (_tileEntity.GetChunk() != null) // no chunk no block
                    {
                        var blockName = _tileEntity.blockValue.Block.blockName;
                        ApplyBonus = _containerTags.Test_AnySet(Safes)
                            || blockName.StartsWith("cntLootCrate")
                            || blockName.StartsWith("cntLootChest")
                            || blockName.StartsWith("cntHardened")
                            || blockName.StartsWith("cntBuried")
                            || blockName.StartsWith("cntWeapon")
                            || blockName.StartsWith("cntAmmo")
                            || blockName.StartsWith("cntChem")
                            || blockName.StartsWith("cntMedic")
                            || blockName.StartsWith("cntFood")
                            || blockName.StartsWith("cntLootWeapon")
                            || blockName.StartsWith("cntLootTools")
                            || blockName.StartsWith("cntBackpack");

                        if (ApplyBonus)
                        {
                            var world = GameManager.Instance.World;
                            var player = world.GetEntity(_entityIdThatOpenedIt) as EntityPlayer;
                            var prefab = player?.prefab?.prefab;
                            PoiTier = Mathf.Max(0, prefab?.DifficultyTier ?? 0);
                            LogModInfo($"Container: {blockName}, Tags: [{_containerTags}], POI Tier: {PoiTier}, Bonus applied.");
                        }
                        else
                        {
                            //LogModError($"Container: {blockName}, Tags: [{_containerTags}], Bonus was not applied.");
                        }
                    }
                }

                public static void Postfix()
                {
                    ApplyBonus = false;
                }
            }
        }
    }
}
