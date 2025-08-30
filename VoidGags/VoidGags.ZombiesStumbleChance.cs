using System;
using HarmonyLib;
using static VoidGags.VoidGags.ZombiesStumbleChance;

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
            Chance = Settings.ZombiesStumbleChance / 200f;

            Harmony.Patch(AccessTools.Method(typeof(EntityStats), nameof(EntityStats.UpdateNPCStatsOverTime)),
                prefix: new HarmonyMethod(EntityStats_UpdateNPCStatsOverTime.Prefix));
        }

        public static class ZombiesStumbleChance
        {
            public static float Chance = 0f;

            /// <summary>
            /// Sometimes zombies can stumble and fall while moving.
            /// </summary>
            public static class EntityStats_UpdateNPCStatsOverTime
            {
                public static void Prefix(EntityAlive ___m_entity)
                {
                    var zombie = ___m_entity as EntityZombie;
                    if (zombie != null && zombie.rand.RandomFloat < Chance)
                    {
                        // check state: not ragdoll, is moving, etc.
                        if (zombie.emodel?.IsRagdollActive == false && zombie.speedForward >= 0.03f && zombie.onGround && !zombie.IsInWater())
                        {
                            //Debug.LogWarning("Stumble activated for " + zombie.EntityName);
                            var dmgResponse = DamageResponse.New(
                                new DamageSource(EnumDamageSource.External, EnumDamageTypes.Bashing, zombie.transform.forward),
                                _fatal: false);
                            dmgResponse.Strength = Math.Min((int)(zombie.speedForward * 400), 250);
                            zombie.emodel.DoRagdoll(dmgResponse, 1f);
                        }
                    }
                }
            }
        }
    }
}
