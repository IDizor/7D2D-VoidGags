using HarmonyLib;
using UnityEngine;

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
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((float abundance) => LootContainer_SpawnLootItemsFromList.Prefix(ref abundance))));

            Harmony.Patch(AccessTools.Method(typeof(LootManager), nameof(LootManager.LootContainerOpened)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((LootManager_LootContainerOpened.APrefix p) => LootManager_LootContainerOpened.Prefix(p._tileEntity, p._entityIdThatOpenedIt, p._containerTags))),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => LootManager_LootContainerOpened.Postfix())));
        }

        public static class MainLootTierBonus
        {
            public static FastTags<TagGroup.Global> Safes = FastTags<TagGroup.Global>.Parse("safes");

            public static bool ApplyBonus = false;
            public static int PoiTier = 0;
        }

        /// <summary>
        /// Applies better tier loot bonus for the main loot containers.
        /// </summary>
        public class LootContainer_SpawnLootItemsFromList
        {
            public static void Prefix(ref float abundance)
            {
                if (MainLootTierBonus.ApplyBonus)
                {
                    MainLootTierBonus.ApplyBonus = false;
                    if (MainLootTierBonus.PoiTier > 0)
                    {
                        abundance *= 1.2f + (0.2f * (MainLootTierBonus.PoiTier - 1));
                    }
                }
            }
        }

        /// <summary>
        /// Checks if opened container can be considered as a main loot container and allows to apply bonus to it.
        /// </summary>
        public class LootManager_LootContainerOpened
        {
            public struct APrefix
            {
                public ITileEntityLootable _tileEntity;
                public int _entityIdThatOpenedIt;
                public FastTags<TagGroup.Global> _containerTags;
            }

            public static void Prefix(ITileEntityLootable _tileEntity, int _entityIdThatOpenedIt, FastTags<TagGroup.Global> _containerTags)
            {
                if (_tileEntity.GetChunk() != null) // no chunk no block
                {
                    var blockName = _tileEntity.blockValue.Block.blockName;
                    MainLootTierBonus.ApplyBonus = _containerTags.Test_AnySet(MainLootTierBonus.Safes);
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntLootCrate");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntLootChest");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntHardened");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntBuried");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntWeapon");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntAmmo");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntChem");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntMedic");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntFood");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntLootWeapon");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntLootTools");
                    MainLootTierBonus.ApplyBonus = MainLootTierBonus.ApplyBonus || blockName.StartsWith("cntBackpack");

                    if (MainLootTierBonus.ApplyBonus)
                    {
                        var world = GameManager.Instance.World;
                        var player = world.GetEntity(_entityIdThatOpenedIt) as EntityPlayer;
                        var prefab = player?.prefab?.prefab;
                        MainLootTierBonus.PoiTier = Mathf.Max(0, prefab?.DifficultyTier ?? 0);
                        LogModWarning($"Container: {blockName}, Tags: [{_containerTags}], POI Tier: {MainLootTierBonus.PoiTier}, Bonus applied.");
                    }
                    else
                    {
                        //LogModError($"Container: {blockName}, Tags: [{_containerTags}], Bonus was not applied.");
                    }
                }
            }

            public static void Postfix()
            {
                MainLootTierBonus.ApplyBonus = false;
            }
        }
    }
}
