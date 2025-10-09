using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using VoidGags.Types;
using static VoidGags.VoidGags.VisibleScriptedSleepers;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_VisibleScriptedSleepers()
        {
            LogApplyingPatch(nameof(Settings.VisibleScriptedSleepers));

            Harmony.Patch(AccessTools.Method(typeof(TriggerVolume), nameof(TriggerVolume.CheckTouching)),
                postfix: new HarmonyMethod(TriggerVolume_CheckTouching.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(SleeperVolume), nameof(SleeperVolume.WakeAttackLater)),
                prefix: new HarmonyMethod(SleeperVolume_WakeAttackLater.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(SleeperEventData), nameof(SleeperEventData.SetupData)),
                postfix: new HarmonyMethod(SleeperEventData_SetupData.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EntityPlayerLocal), nameof(EntityPlayerLocal.OnUpdateLive)),
                prefix: new HarmonyMethod(EntityPlayerLocal_OnUpdateLive.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(EAISetNearestEntityAsTarget), nameof(EAISetNearestEntityAsTarget.FindTargetPlayer)),
                prefix: new HarmonyMethod(EAISetNearestEntityAsTarget_FindTargetPlayer.Prefix));
        }

        public static class VisibleScriptedSleepers
        {
            public const float TouchDistance = 20f;
            public const float SeeDistance = 100f;
            public static int PlayerId;
            public static float TouchTime = 0f;

            /// <summary>
            /// Spawn scripted sleepers on reasonable distance.
            /// </summary>
            public static class TriggerVolume_CheckTouching
            {
                public static void Postfix(TriggerVolume __instance, World _world, EntityPlayer _player)
                {
                    if (!__instance.isTriggered)
                    {
                        if (_player.position.DistanceTo(__instance.Center) < TouchDistance ||
                            Helper.PlayerCanSeePos(_player, __instance.Center))
                        {
                            PlayerId = _player.entityId;
                            TouchTime = Time.time;
                            __instance.Touch(_world, _player);
                        }
                    }
                }
            }

            /// <summary>
            /// Disable sleepers auto-attack until they see/hear the player.
            /// </summary>
            public static class SleeperVolume_WakeAttackLater
            {
                public static bool Prefix(EntityAlive _ea, EntityPlayer _playerTouched, ref IEnumerator __result)
                {
                    var time = Time.time;
                    if (PlayerId == _playerTouched.entityId && time - TouchTime < 2f)
                    {
                        __result = CheckStandingOnBlock(_ea);
                        return false;
                    }

                    return true;

                    static IEnumerator CheckStandingOnBlock(EntityAlive entity)
                    {
                        yield return new WaitForSeconds(1f);
                        if (entity?.blockValueStandingOn.Block is BlockTrapDoor weakBlock)
                        {
                            // touch weak block in random time
                            yield return new WaitForSeconds(entity.rand.Next(30, 60));
                            if (entity != null && weakBlock != null)
                            {
                                entity.SetSleeperActive();
                                weakBlock.OnEntityCollidedWithBlock(entity.world, 0, entity.blockPosStandingOn, entity.blockValueStandingOn, entity);
                            }
                        }
                        else
                        {
                            // activate sleepers senses during sleep
                            yield return new WaitForSeconds(entity.rand.Next(30, 60));
                            if (entity != null && !entity.IsDead())
                            {
                                entity.SetSleeperActive();
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Show more not-killed sleepers on compass, in case some zombies cannot hear you from hidden housing.
            /// </summary>
            public static class SleeperEventData_SetupData
            {
                public static void Postfix(SleeperEventData __instance)
                {
                    __instance.ShowQuestClearCount = Mathf.CeilToInt(__instance.ShowQuestClearCount * 1.5f);
                }
            }

            /// <summary>
            /// Spawn regular sleepers.
            /// </summary>
            public static class EntityPlayerLocal_OnUpdateLive
            {
                private const int svChunks = 4;
                private const float svChunkTime = 0.5f;
                private static DelayStorage Delay = new(svChunkTime * svChunks);
                private static DelayStorage DelayClosest = new(15f);
                private static List<SleeperVolume> ClosestVolumes = [];

                public static void Postfix(EntityPlayerLocal __instance)
                {
                    if (DelayClosest.Check())
                    {
                        var world = GameManager.Instance.World;
                        ClosestVolumes = world.sleeperVolumes
                            .Where(sv => sv.BoxMin.IsInCubeWith(__instance.blockPosStandingOn, 300))
                            .ToList();
                    }
                    if (Delay.Check())
                    {
                        var svChunkSize = (ClosestVolumes.Count / svChunks) + 1;
                        if (svChunkSize > 1)
                        {
                            var world = GameManager.Instance.World;
                            GameManager.Instance.StartCoroutine(ProcessSleeperVolumes());
                            IEnumerator ProcessSleeperVolumes()
                            {
                                for (int i = 0; i < svChunks; i++)
                                {
                                    if (world == null || __instance == null) break;
                                    ProcessSvChunk(world, svChunkSize, i, __instance);
                                    if (i < svChunks - 1)
                                        yield return new WaitForSeconds(svChunkTime);
                                }
                            }
                        }
                    }
                }

                /// <summary>
                /// Have to process sleeper volumes by chunks for better performance.
                /// </summary>
                private static void ProcessSvChunk(World world, int chunkSize, int chunkNumber, EntityPlayerLocal player)
                {
                    var sleeperVolumes = ClosestVolumes
                        .Skip(chunkSize * chunkNumber).Take(chunkSize)
                        .Where(sv => sv != null && !sv.wasCleared && !sv.isSpawned && !sv.isSpawning)
                        .ToArray();

                    foreach (var sleepers in sleeperVolumes)
                    {
                        var distance = player.position.DistanceTo(sleepers.Center);
                        if (distance > SeeDistance) continue;
                        if (distance < TouchDistance)
                        {
                            sleepers.UpdatePlayerTouched(world, player);
                        }
                        else
                        {
                            var positions = new List<Vector3>() { sleepers.Center };
                            if (sleepers.respawnMap?.Count > 0)
                            {
                                positions.AddRange(sleepers.respawnMap
                                    .Select(r => world.GetEntity(r.Key) as EntityAlive)
                                    .Where(s => s?.IsDead() == false)
                                    .Select(s => s.position));
                            }
                            if (positions.Any(pos => Helper.PlayerCanSeePos(player, pos)))
                            {
                                sleepers.UpdatePlayerTouched(world, player);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Activate passive sleepers / their senses.
            /// </summary>
            public static class EAISetNearestEntityAsTarget_FindTargetPlayer
            {
                private static DelayStorage<int> Delays = new(2f);

                public static void Prefix(EAISetNearestEntityAsTarget __instance)
                {
                    if (__instance.theEntity?.IsSleeperPassive == true && Delays.Check(__instance.theEntity.entityId))
                    {
                        if (Helper.AnyPlayerCanSeePos(__instance.theEntity.world, [__instance.theEntity.position], SeeDistance, out _))
                        {
                            __instance.theEntity?.SetSleeperActive();
                        }
                    }
                }
            }
        }
    }
}
