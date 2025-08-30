using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.GrenadesDistraction;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_GrenadesDistraction()
        {
            LogApplyingPatch(nameof(Settings.GrenadesDistraction));
            UseXmlPatches(nameof(Settings.GrenadesDistraction));

            Harmony.Patch(AccessTools.Method(typeof(EntityItem), nameof(EntityItem.tickDistraction)),
                prefix: new HarmonyMethod(EntityItem_tickDistraction.Prefix));
        }

        public static class GrenadesDistraction
        {
            /// <summary>
            /// Thrown distraction items can wake sleeping zombies.
            /// </summary>
            public static class EntityItem_tickDistraction
            {
                public static void Prefix(EntityItem __instance, int ___distractionLifetime, float ___distractionRadiusSq, int ___nextDistractionTick)
                {
                    if (___nextDistractionTick > 0 && ___nextDistractionTick % 5 == 0)
                    {
                        if (__instance.itemClass != null && ___distractionLifetime > 0 && __instance.isCollided && __instance.itemClass.IsRequireContactDistraction && ___distractionRadiusSq > 0f)
                        {
                            var radius = Mathf.Sqrt(___distractionRadiusSq) / 4f; // div by 4f to shrink full distraction area for sleepers
                            var targetsToWakeUp = Helper.GetEntities<EntityEnemy>(__instance.position, radius);

                            foreach (var entityEnemy in targetsToWakeUp)
                            {
                                if (entityEnemy.IsSleeping)
                                {
                                    var occlusion = Helper.CalculateNoiseOcclusion(__instance.position, entityEnemy.position, 0.03f);
                                    if (occlusion >= 0.8f)
                                    {
                                        entityEnemy.ConditionalTriggerSleeperWakeUp();
                                    }
                                }
                            }
                            targetsToWakeUp.Clear();
                        }
                    }
                }
            }
        }
    }
}
