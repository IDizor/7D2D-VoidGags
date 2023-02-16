using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_AirDropIsNeverEmpty(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(LootManager), "LootContainerOpened"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((TileEntityLootContainer _tileEntity) => LootManager_LootContainerOpened.Prefix(_tileEntity))),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => LootManager_LootContainerOpened.Postfix())));

            harmony.Patch(AccessTools.Method(typeof(LootContainer), "Spawn"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((float playerLevelPercentage) => LootContainer_Spawn.Prefix(ref playerLevelPercentage))),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((LootContainer_Spawn_Postfix p) => LootContainer_Spawn.Postfix(
                    p.__instance, ref p.__result, p.random, p._maxItems, p.playerLevelPercentage, p.rareLootChance, p.player, p.containerTags))));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.AirDropIsNeverEmpty)}");
        }

        private static bool IsAirDrop = false;

        private struct LootContainer_Spawn_Postfix
        {
            public LootContainer __instance;
            public IList<ItemStack> __result;
            public GameRandom random;
            public int _maxItems;
            public float playerLevelPercentage;
            public float rareLootChance;
            public EntityPlayer player;
            public FastTags containerTags;
        }

        /// <summary>
        /// Sets the <see cref="IsAirDrop"/> flag that indicates whether the air drop loot is generating now.
        /// </summary>
        public class LootManager_LootContainerOpened
        {
            public static void Prefix(TileEntityLootContainer _tileEntity)
            {
                IsAirDrop = _tileEntity.lootListName == "airDrop";
            }

            public static void Postfix()
            {
                IsAirDrop = false;
            }
        }

        /// <summary>
        /// Repeats spawn for supply crate until anything spawned.
        /// </summary>
        public class LootContainer_Spawn
        {
            public static void Prefix(ref float playerLevelPercentage)
            {
                if (IsAirDrop)
                {
                    playerLevelPercentage += 20;
                }
            }

            public static void Postfix(LootContainer __instance, ref IList<ItemStack> __result, GameRandom random, int _maxItems, float playerLevelPercentage, float rareLootChance, EntityPlayer player, FastTags containerTags)
            {
                if (IsAirDrop)
                {
                    IsAirDrop = false;
                    int n = 0;
                    while (__result.Count == 0 && n < 99999)
                    {
                        __result = __instance.Spawn(random, _maxItems, playerLevelPercentage, rareLootChance, player, containerTags);
                        n++;
                    }
                }
            }
        }
    }
}
