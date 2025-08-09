using System;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ZombiesStumbleChance()
        {
            LogApplyingPatch(nameof(Settings.ZombiesStumbleChance));

            // divided by 200, not by 100, because UpdateNPCStatsOverTime() is called twice per second.
            ZombiesStumbleChance.Chance = Settings.ZombiesStumbleChance / 200f;

            Harmony.Patch(AccessTools.Method(typeof(EntityStats), nameof(EntityStats.UpdateNPCStatsOverTime)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive ___m_entity) => EntityStats_UpdateNPCStatsOverTime.Prefix(___m_entity))));
        }

        public static class ZombiesStumbleChance
        {
            public static float Chance = 0f;
        }

        /// <summary>
        /// Sometimes zombies can stumble and fall while moving.
        /// </summary>
        public class EntityStats_UpdateNPCStatsOverTime
        {
            public static void Prefix(EntityAlive ___m_entity)
            {
                var zombie = ___m_entity as EntityZombie;
                if (zombie != null && zombie.rand.RandomFloat < ZombiesStumbleChance.Chance)
                {
                    // if not ragdoll and is moving
                    if (zombie.emodel?.IsRagdollOn == false && zombie.speedForward >= 0.03f)
                    {
                        var dmgResponse = DamageResponse.New(
                            new DamageSource(EnumDamageSource.External, EnumDamageTypes.Bashing, zombie.transform.forward + Vector3.down),
                            _fatal: false);
                        dmgResponse.Strength = Math.Min((int)(zombie.speedForward * 1000), 250);
                        //LogModWarning($"speed = {zombie.speedForward:0.000}, str = {dmgResponse.Strength}");
                        zombie.emodel.DoRagdoll(dmgResponse, 1f);
                    }
                }
            }
        }
    }
}
