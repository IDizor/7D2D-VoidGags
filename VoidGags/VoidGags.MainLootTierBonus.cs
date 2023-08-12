using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_MainLootTierBonus(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(LootContainer), "SpawnLootItemsFromList"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((float abundance) => LootContainer_SpawnLootItemsFromList.Prefix(ref abundance))));

            harmony.Patch(AccessTools.Method(typeof(LootManager), "LootContainerOpened"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((LootManager_LootContainerOpened_2.APrefix p) => LootManager_LootContainerOpened_2.Prefix(p._tileEntity, p._entityIdThatOpenedIt, p._containerTags))),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => LootManager_LootContainerOpened_2.Postfix())));

            LogPatchApplied(nameof(Settings.MainLootTierBonus));
        }

        /// <summary>
        /// Applies better tier loot bonus for the main loot containers.
        /// </summary>
        public class LootContainer_SpawnLootItemsFromList
        {
            public static void Prefix(ref float abundance)
            {
                if (LootManager_LootContainerOpened_2.ApplyBonus)
                {
                    LootManager_LootContainerOpened_2.ApplyBonus = false;
                    if (LootManager_LootContainerOpened_2.PoiTier > 0)
                    {
                        abundance *= 1f + (0.25f * (LootManager_LootContainerOpened_2.PoiTier - 1));
                    }
                }
            }
        }

        /// <summary>
        /// Checks if opened container can be considered as a main loot container and allows to apply bonus to it.
        /// </summary>
        public class LootManager_LootContainerOpened_2
        {
            public static FastTags Safes = FastTags.Parse("safes");

            public static bool ApplyBonus = false;
            public static int PoiTier = 0;

            public struct APrefix
            {
                public TileEntityLootContainer _tileEntity;
                public int _entityIdThatOpenedIt;
                public FastTags _containerTags;
            }

            public static void Prefix(TileEntityLootContainer _tileEntity, int _entityIdThatOpenedIt, FastTags _containerTags)
            {
                if (_tileEntity.GetChunk() != null) // no chunk no block
                {
                    var blockName = _tileEntity.blockValue.Block.GetBlockName();
                    ApplyBonus = _containerTags.Test_AnySet(Safes);
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntLootCrate");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntLootChest");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntHardened");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntBuried");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntWeapon");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntAmmo");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntChem");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntMedic");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntFood");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntLootWeapon");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntLootTools");
                    ApplyBonus = ApplyBonus || blockName.StartsWith("cntBackpack");

                    if (ApplyBonus)
                    {
                        var world = GameManager.Instance.World;
                        var player = (EntityPlayer)world.GetEntity(_entityIdThatOpenedIt);
                        var prefab = player.prefab?.prefab;
                        PoiTier = Mathf.Max(0, prefab?.DifficultyTier ?? 0);
                        Debug.LogWarning($"Container: {blockName}, Tags: [{_containerTags}], POI Tier: {PoiTier}");
                    }
                    else
                    {
                        Debug.Log($"Container: {blockName}, Tags: [{_containerTags}], Bonus was not applied.");
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
