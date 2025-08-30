using HarmonyLib;
using UnityEngine;
using VoidGags.NetPackages;
using static VoidGags.VoidGags.ExplosionAttractionFix;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ExplosionAttractionFix()
        {
            LogApplyingPatch(nameof(Settings.ExplosionAttractionFix));

            Harmony.Patch(AccessTools.Method(typeof(AIDirector), nameof(AIDirector.OnSoundPlayedAtPosition)),
                prefix: new HarmonyMethod(AIDirector_OnSoundPlayedAtPosition.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.explode)),
                prefix: new HarmonyMethod(GameManager_explode.Prefix),
                postfix: new HarmonyMethod(GameManager_explode.Postfix));
        }

        public static class ExplosionAttractionFix
        {
            public const float WakeRadius = 15f;
            public const float AttractionRadius = 50f;
            public static bool IsExplosion = false;

            /// <summary>
            /// Sends zombies to check explosion location, and not the player location.
            /// </summary>
            public static class AIDirector_OnSoundPlayedAtPosition
            {
                public static void Prefix(AIDirector __instance, ref int _entityThatCausedSound, Vector3 _position)
                {
                    if (IsExplosion)
                    {
                        _entityThatCausedSound = -1;
                        var attractionTargets = Helper.GetEntities<EntityEnemy>(_position, AttractionRadius);
                        var targetsToWakeUp = Helper.GetEntities<EntityEnemy>(_position, WakeRadius);

                        foreach (var entityEnemy in attractionTargets)
                        {
                            if (entityEnemy.distraction != null && entityEnemy.distraction.position != _position)
                            {
                                continue;
                            }

                            if (entityEnemy.IsSleeping && targetsToWakeUp.Contains(entityEnemy))
                            {
                                entityEnemy.ConditionalTriggerSleeperWakeUp();
                            }

                            if (!entityEnemy.IsSleeping)
                            {
                                int ticks = entityEnemy.CalcInvestigateTicks((int)(30f + __instance.random.RandomFloat * 30f) * 20, null);
                                Vector2 vector = __instance.random.RandomInsideUnitCircle * 7f; // circle * radius
                                var randomizedPos = _position + new Vector3(vector.x, 0f, vector.y);
                                entityEnemy.SetInvestigatePosition(randomizedPos, ticks);
                                SingletonMonoBehaviour<ConnectionManager>.Instance.SendToClientsOrServer(NetPackageManager.GetPackage<NetPackageSetInvestigatePos>().Setup(entityEnemy.entityId, randomizedPos, ticks));
                            }
                        }
                        attractionTargets.Clear();
                        targetsToWakeUp.Clear();
                    }
                }
            }

            /// <summary>
            /// Track explosion moment.
            /// </summary>
            public static class GameManager_explode
            {
                public static void Prefix(ItemValue _itemValueExplosionSource)
                {
                    if (_itemValueExplosionSource?.ItemClass != null)
                    {
                        // filter zombies ranged attacks
                        IsExplosion = !_itemValueExplosionSource.ItemClass.Name.ContainsCaseInsensitive("meleeHand");
                    }
                }

                public static void Postfix()
                {
                    IsExplosion = false;
                }
            }
        }
    }
}
