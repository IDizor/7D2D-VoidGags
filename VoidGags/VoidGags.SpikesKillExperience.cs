using HarmonyLib;
using static VoidGags.VoidGags.SpikesKillExperience;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_SpikesKillExperience()
        {
            LogApplyingPatch(nameof(Settings.SpikesKillExperience));

            Distance = Settings.SpikesKillExperience;

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.Kill)),
                prefix: new HarmonyMethod(EntityAlive_Kill.Prefix));
        }

        public static class SpikesKillExperience
        {
            public static float Distance = 10f;

            /// <summary>
            /// Closest player gets experience for killing zombies using spikes traps.
            /// </summary>
            public static class EntityAlive_Kill
            {
                public static void Prefix(EntityAlive __instance, ref DamageResponse _dmResponse)
                {
                    if (__instance is EntityEnemy && _dmResponse.Source?.AttackingItem != null && _dmResponse.Source.ownerEntityId <= 0)
                    {
                        var itemBlock = _dmResponse.Source.AttackingItem.ItemClass.GetBlock();
                        if (itemBlock != null && PickupSpikes.IsSpikesTrap(itemBlock))
                        {
                            var player = Helper.GetClosestPlayer(GameManager.Instance.World, __instance.position, Distance);
                            if (player != null)
                            {
                                _dmResponse.Source.ownerEntityId = player.entityId;
                                player.AddKillXP(__instance, itemUsed: null);
                                //LogWarning($"Spikes kill with player. Enemy killed: {__instance.entityName}");
                            }
                        }
                    }
                }
            }
        }
    }
}
