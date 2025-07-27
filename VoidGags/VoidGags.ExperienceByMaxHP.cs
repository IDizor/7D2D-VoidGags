using System.Collections.Concurrent;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ExperienceByMaxHP()
        {
            LogApplyingPatch(nameof(Settings.ExperienceRewardByMaxHP));

            if (Settings.ExperienceRewardByMaxHP_Multiplier >= 0)
            {
                EntityPlayer_AddKillXP.ExpMultiplier = Settings.ExperienceRewardByMaxHP_Multiplier;

                Harmony.Patch(AccessTools.Method(typeof(EntityPlayer), nameof(EntityPlayer.AddKillXP)),
                    prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive killedEntity) => EntityPlayer_AddKillXP.Prefix(killedEntity))),
                    postfix:new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive killedEntity) => EntityPlayer_AddKillXP.Postfix(killedEntity))));

            }
            else
            {
                LogModException($"Invalid value for setting '{nameof(Settings.ExperienceRewardByMaxHP_Multiplier)}'.");
            }
        }

        /// <summary>
        /// Experience reward for killing zombies depends on their max health and armor rate. Plus a little random.
        /// </summary>
        public class EntityPlayer_AddKillXP
        {
            public static float ExpMultiplier = 1.0f;
            public static ConcurrentDictionary<string, int> EntityExperience = new ConcurrentDictionary<string, int>();

            public static void Prefix(EntityAlive killedEntity)
            {
                if (killedEntity.EntityClass.ExperienceValue > 0)
                {
                    EntityExperience.TryAdd(killedEntity.EntityName, killedEntity.EntityClass.ExperienceValue);

                    var expFor1HP = killedEntity is EntityVulture ? 18f
                        : killedEntity is EntityAnimalSnake ? 15f
                        : killedEntity is EntityZombieDog ? 6f
                        : killedEntity is EntityEnemy ? 3f
                        : 5f;

                    var maxHealth = killedEntity.GetMaxHealth();
                    var armor = EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0, killedEntity);
                    var armorMultiplier = 1 + (armor / 200);
                    var expGrowthLimiter = Mathf.Pow((100f / maxHealth), 0.2f);

                    float exp = maxHealth * expFor1HP * armorMultiplier * expGrowthLimiter + Random.Range(-maxHealth / 8, maxHealth / 8);
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
