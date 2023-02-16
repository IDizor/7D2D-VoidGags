using System.Collections.Concurrent;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ExperienceByMaxHP(Harmony harmony)
        {
            if (Settings.ExperienceRewardByMaxHP_Multiplier >= 0)
            {
                ExpMultiplier = Settings.ExperienceRewardByMaxHP_Multiplier;

                harmony.Patch(AccessTools.Method(typeof(EntityPlayer), "AddKillXP"),
                    new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive killedEntity) => EntityPlayer_AddKillXP.Prefix(killedEntity))),
                    new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive killedEntity) => EntityPlayer_AddKillXP.Postfix(killedEntity))));

                Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.ExperienceRewardByMaxHP)}");
            }
            else
            {
                Debug.LogError($"Mod {nameof(VoidGags)}: Invalid value for setting '{nameof(Settings.ExperienceRewardByMaxHP_Multiplier)}'.");
            }
        }

        private static float ExpMultiplier = 1.0f;
        private static ConcurrentDictionary<string, int> EntityExperience = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// Experience reward for killing zombies depends on their max health and armor rate. Plus a little random.
        /// </summary>
        public class EntityPlayer_AddKillXP
        {
            public static void Prefix(EntityAlive killedEntity)
            {
                if (killedEntity.EntityClass.ExperienceValue > 0)
                {
                    EntityExperience.TryAdd(killedEntity.EntityName, killedEntity.EntityClass.ExperienceValue);

                    var expMultiplier = killedEntity is EntityVulture ? 16f
                        : killedEntity is EntityAnimalSnake ? 15f
                        : killedEntity is EntityZombieDog ? 5f
                        : killedEntity is EntityEnemy ? 3f
                        : 5f;

                    var maxHealth = killedEntity.GetMaxHealth();
                    var armorMultiplier = EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0, killedEntity);
                    armorMultiplier = 1 + (armorMultiplier / 100);

                    float exp = maxHealth * expMultiplier * armorMultiplier + Random.Range(-maxHealth / 6, maxHealth / 4);
                    killedEntity.EntityClass.ExperienceValue = (int)(exp * ExpMultiplier);
                }
            }

            public static void Postfix(EntityAlive killedEntity)
            {
                if (EntityExperience.TryGetValue(killedEntity.EntityName, out int experience))
                {
                    killedEntity.EntityClass.ExperienceValue = experience;
                }
            }
        }
    }
}
