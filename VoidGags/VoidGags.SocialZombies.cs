using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using VoidGags.NetPackages;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_SocialZombies()
        {
            LogApplyingPatch(nameof(Settings.SocialZombies));

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.ProcessDamageResponseLocal)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive_ProcessDamageResponseLocal.APrefix p) => EntityAlive_ProcessDamageResponseLocal.Prefix(p._dmResponse, p.__instance, ref p.___soundDeath, ref p.___soundRandom, ref p.___soundAttack, ref p.___soundAttack, ref p.___soundHurt))));

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.GetSoundDeath)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive_GetSoundDeath.APrefix p) => EntityAlive_GetSoundDeath.Prefix(p.__instance, ref p.__result))));

            Harmony.Patch(AccessTools.Method(typeof(EntityEnemy), nameof(EntityEnemy.DamageEntity)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityEnemy __instance) => EntityEnemy_DamageEntity.Prefix(__instance))),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityEnemy __instance) => EntityEnemy_DamageEntity.Postfix(__instance))));

            Harmony.Patch(AccessTools.Method(typeof(Entity), nameof(Entity.PlayOneShot)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((Entity_PlayOneShot.APostfix p) => Entity_PlayOneShot.Postfix(p.__instance, p.clipName))));

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.ConditionalTriggerSleeperWakeUp)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive __instance) => EntityAlive_ConditionalTriggerSleeperWakeUp.Prefix(__instance))));

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.SetInvestigatePosition)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive_SetInvestigatePosition.APrefix p) => EntityAlive_SetInvestigatePosition.Prefix(p.__instance, p.isAlert, out p.__state))),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive_SetInvestigatePosition.APostfix p) => EntityAlive_SetInvestigatePosition.Postfix(p.__instance, p.__state))));

            Harmony.Patch(AccessTools.Method(typeof(EntityAlive), nameof(EntityAlive.SetAttackTarget)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityAlive __instance) => EntityAlive_SetAttackTarget.Prefix(__instance))));
        }

        /// <summary>
        /// Helper class.
        /// </summary>
        public static class SocialZombies
        {
            public static void AttractZombiesAround(EntityAlive entity, float delay = 0.5f, float radius = 6f, Func<float> delayForEach = null)
            {
                if (entity == null)
                {
                    return;
                }

                var entityPosition = entity.position;
                var loafers = Helper.GetEntities<EntityEnemy>(entityPosition, radius)
                    .Where(e => e != entity && !(e is EntityFlying) && !e.IsDead() && (e.IsSleeping || !e.IsBusy()))
                    .Where(e => Helper.CalculateNoiseOcclusion(entityPosition, e.position, 0.027f) > 0.85f)
                    .ToArray();

                if (loafers.Length > 0)
                {
                    Helper.DeferredAction(delay, () =>
                    {
                        //Debug.LogError($"loafers {loafers.Length}");
                        if (entity != null && !entity.IsDead())
                        {
                            var attackTarget = entity.GetAttackTarget();
                            var attackTime = attackTarget == null ? 0 : entity.attackTargetTime;
                            var isInvestigating = entity.IsInvestigating();
                            var investigatingPosition = entity.InvestigatePosition;
                            var delays = delayForEach == null ? null : loafers.Select(_ => delayForEach()).ToList();
                            delays?.Sort();

                            for (int i = 0; i < loafers.Length; i++)
                            {
                                var loafer = loafers[i];
                                var isLast = i == loafers.Length - 1;
                                
                                //Debug.LogWarning($"[{i+1}] IsSleeping {loafer.IsSleeping}, IsBusy {loafer.IsBusy()}, attackTarget {attackTarget != null}, attackTime {attackTime > 0}");
                                if (delays == null)
                                {
                                    Attract();
                                }
                                else
                                {
                                    Helper.DeferredAction(delays[i], Attract);
                                }

                                void Attract()
                                {
                                    if (loafer == null || loafer.IsDead())
                                    {
                                        return;
                                    }
                                    if (loafer.IsSleeping)
                                    {
                                        loafer.ConditionalTriggerSleeperWakeUp();
                                    }
                                    if (!loafer.IsBusy())
                                    {
                                        if (attackTarget != null && attackTime > 0)
                                        {
                                            loafer.SetRevengeTarget(attackTarget);
                                            loafer.SetRevengeTimer(10000);
                                        }
                                        else if (isInvestigating)
                                        {
                                            if (isLast)
                                            {
                                                // call original method to allow its patches to run
                                                //Debug.LogError($"{Time.time:0.00} SetInvestigatePosition");
                                                loafer.SetInvestigatePosition(investigatingPosition, 600, true);
                                            }
                                            else
                                            {
                                                // call pseudo method
                                                //Debug.LogWarning($"{Time.time:0.00} InvestigatePosition");
                                                InvestigatePosition(loafer, investigatingPosition, 600);
                                            }
                                            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToClientsOrServer(NetPackageManager.GetPackage<NetPackageSetInvestigatePos>().Setup(loafer.entityId, investigatingPosition, 600));
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }

            /// <summary>
            /// Own method to set investigate position to avoid recursive Harmony patches calls.
            /// Should contain the same code as the original method <see cref="EntityAlive.SetInvestigatePosition(Vector3, int, bool)"/>.
            /// TODO: Check the original method before new mod version release.
            /// </summary>
            private static void InvestigatePosition(EntityAlive entity, Vector3 pos, int ticks)
            {
                entity.investigatePos = pos;
                entity.investigatePositionTicks = ticks;
                entity.isInvestigateAlert = true;
            }
        }

        /// <summary>
        /// No head no screams.
        /// (but still unable to stop sounds started before the dismembering)
        /// </summary>
        public class EntityAlive_ProcessDamageResponseLocal
        {
            public struct APrefix
            {
                public DamageResponse _dmResponse;
                public EntityAlive __instance;
                public string ___soundDeath;
                public string ___soundRandom;
                public string ___soundAttack;
                public string ___soundAlert;
                public string ___soundHurt;
            }

            public static void Prefix(DamageResponse _dmResponse, EntityAlive __instance,
                ref string ___soundDeath, ref string ___soundRandom, ref string ___soundAttack, ref string ___soundAlert, ref string ___soundHurt)
            {
                if (_dmResponse.Dismember && _dmResponse.HitBodyPart == EnumBodyPartHit.Head)
                {
                    __instance.soundSense = ___soundDeath = ___soundRandom = ___soundAttack = ___soundAlert = ___soundHurt = null;
                }
            }
        }

        /// <summary>
        /// No death sound when sleeper is killed with one shot.
        /// </summary>
        public class EntityAlive_GetSoundDeath
        {
            public struct APrefix
            {
                public EntityAlive __instance;
                public string __result;
            }

            public static bool Prefix(EntityAlive __instance, ref string __result)
            {
                if (__instance is EntityEnemy)
                {
                    if (EntityEnemy_DamageEntity.PreventDeathSound)
                    {
                        __result = null;
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Tracks sleeping state to prevent zombie death sound.
        /// </summary>
        public class EntityEnemy_DamageEntity
        {
            public static bool PreventDeathSound = false;

            public static void Prefix(EntityEnemy __instance)
            {
                PreventDeathSound = __instance.IsSleeping;
            }

            public static void Postfix(EntityEnemy __instance)
            {
                PreventDeathSound = __instance.IsSleeping;
            }
        }

        /// <summary>
        /// Wake bros when player detected.
        /// </summary>
        public class Entity_PlayOneShot
        {
            public struct APostfix
            {
                public Entity __instance;
                public string clipName;
            }

            public static void Postfix(Entity __instance, string clipName)
            {
                if (__instance is EntityEnemy enemy && clipName == enemy.soundSense)
                {
                    SocialZombies.AttractZombiesAround(enemy);
                }
            }
        }

        /// <summary>
        /// Wake bros on wake up.
        /// </summary>
        public class EntityAlive_ConditionalTriggerSleeperWakeUp
        {
            public static void Prefix(EntityAlive __instance)
            {
                if (__instance.IsSleeping && !(__instance is EntityFlying) && __instance is EntityEnemy enemy)
                {
                    Helper.DeferredAction(enemy.rand.RandomRange(0.1f, 1f), () =>
                    { 
                        SocialZombies.AttractZombiesAround(enemy);
                    });
                }
            }
        }

        /// <summary>
        /// Call bros to investigate position.
        /// </summary>
        public class EntityAlive_SetInvestigatePosition
        {
            public struct APrefix
            {
                public EntityAlive __instance;
                public bool isAlert;
                public bool __state;
            }

            public static void Prefix(EntityAlive __instance, bool isAlert, out bool __state)
            {
                __state = isAlert
                    && __instance is EntityEnemy
                    && !(__instance is EntityFlying)
                    && !__instance.IsBusy();
            }

            public struct APostfix
            {
                public EntityAlive __instance;
                public bool __state;
            }

            public static void Postfix(EntityAlive __instance, bool __state)
            {
                if (__state)
                {
                    Helper.DoWhen(() =>
                    {
                        var random = __instance.rand;
                        SocialZombies.AttractZombiesAround(__instance, delayForEach: () => random.RandomRange(0.5f, 2f));
                    }, __instance.CanMove, 0.33f, 7f);
                }
            }
        }

        /// <summary>
        /// Call bros on attack start.
        /// </summary>
        public class EntityAlive_SetAttackTarget
        {
            static Dictionary<int, float> EntityIdAttackTime = new Dictionary<int, float>();

            public static void Prefix(EntityAlive __instance)
            {
                if (!(__instance is EntityFlying) && __instance is EntityEnemy enemy)
                {
                    var time = Time.time;
                    EntityIdAttackTime.RemoveAll((float t) => time - t > 3f);
                    var isAttackStart = enemy.GetAttackTarget() == null;
                    if (isAttackStart || !EntityIdAttackTime.ContainsKey(enemy.entityId))
                    {
                        EntityIdAttackTime[enemy.entityId] = time;
                        Helper.DoWhen(() =>
                        {
                            var random = enemy.rand;
                            SocialZombies.AttractZombiesAround(enemy, delayForEach: () => random.RandomRange(0.2f, 1f));
                        }, enemy.CanMove, 0.33f, 7f);
                    }
                }
            }
        }
    }
}
