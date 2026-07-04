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
            OnTrapChance = Settings.ZombiesStumbleChance_OnSpikes / 200f;

            Harmony.Patch(AccessTools.Method(typeof(EntityStats), nameof(EntityStats.UpdateNPCStatsOverTime)),
                prefix: new HarmonyMethod(EntityStats_UpdateNPCStatsOverTime.Prefix));
        }

        public static class ZombiesStumbleChance
        {
            public static float Chance = 0f;
            public static float OnTrapChance = 0.1f;

            /// <summary>
            /// Sometimes zombies can stumble and fall while moving.
            /// </summary>
            public static class EntityStats_UpdateNPCStatsOverTime
            {
                public static void Prefix(EntityAlive ___m_entity)
                {
                    var zombie = ___m_entity as EntityZombie;
                    if (zombie != null)
                    {
                        var chance = Chance;
                        var stunType = EnumEntityStunType.StumbleBreakThroughRagdoll;
                        var isOnTrap = false;

                        if (OnTrapChance > 0f)
                        {
                            var standingOn = zombie.blockValueStandingOn.Block;
                            isOnTrap = PickupSpikes.IsSpikesTrap(standingOn);
                            if (isOnTrap)
                            {
                                chance = OnTrapChance;
                                stunType = EnumEntityStunType.Kneel;
                            }
                        }

                        if (chance > 0f && zombie.rand.RandomFloat < chance)
                        {
                            // check state: not ragdoll, is moving, etc.
                            if (zombie.emodel?.IsRagdollActive == false && (isOnTrap || zombie.speedForward >= 0.02f) && !zombie.emodel.avatarController.IsAnimationStunRunning() && !zombie.IsWalkTypeACrawl() && zombie.onGround && !zombie.IsInWater())
                            {
                                //Debug.LogWarning("Stumble activated for " + zombie.EntityName);
                                zombie.emodel.avatarController.BeginStun(stunType, EnumBodyPartHit.LeftUpperLeg, Utils.EnumHitDirection.None, _criticalHit: false, 1f);
                                zombie.SetStun(stunType);
                            }
                        }
                    }
                }
            }
        }
    }
}
