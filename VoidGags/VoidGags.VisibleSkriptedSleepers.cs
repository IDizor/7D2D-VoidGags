using System.Collections;
using HarmonyLib;
using UnityEngine;
using VoidGags.Types;

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
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((TriggerVolume_CheckTouching.APostfix p) => TriggerVolume_CheckTouching.Postfix(p.__instance, p._world, p._player))));

            Harmony.Patch(AccessTools.Method(typeof(SleeperVolume), nameof(SleeperVolume.WakeAttackLater)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((SleeperVolume_WakeAttackLater.APrefix p) => SleeperVolume_WakeAttackLater.Prefix(p._ea, p._playerTouched, ref p.__result))));

            Harmony.Patch(AccessTools.Method(typeof(SleeperEventData), nameof(SleeperEventData.SetupData)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((SleeperEventData __instance) => SleeperEventData_SetupData.Postfix(__instance))));

            Harmony.Patch(AccessTools.Method(typeof(SleeperVolume), nameof(SleeperVolume.Tick)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((SleeperVolume_Tick.APrefix p) => SleeperVolume_Tick.Prefix(p.__instance, p._world))));

            Harmony.Patch(AccessTools.Method(typeof(EAISetNearestEntityAsTarget), nameof(EAISetNearestEntityAsTarget.FindTargetPlayer)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((EAISetNearestEntityAsTarget __instance) => EAISetNearestEntityAsTarget_FindTargetPlayer.Prefix(__instance))));
        }

        public static class VisibleScriptedSleepers
        {
            public const float TouchDistance = 15f;
            public static int PlayerId;
            public static float TouchTime = 0f;
        }

        /// <summary>
        /// Spawn scripted sleepers on reasonable distance.
        /// </summary>
        public static class TriggerVolume_CheckTouching
        {
            public struct APostfix
            {
                public TriggerVolume __instance;
                public World _world;
                public EntityPlayer _player;
            }

            public static void Postfix(TriggerVolume __instance, World _world, EntityPlayer _player)
            {
                if (!__instance.isTriggered)
                {
                    if (_player.position.DistanceTo(__instance.Center) < VisibleScriptedSleepers.TouchDistance ||
                        Helper.PlayerCanSeePos(_player, __instance.Center))
                    {
                        VisibleScriptedSleepers.PlayerId = _player.entityId;
                        VisibleScriptedSleepers.TouchTime = Time.time;
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
            public struct APrefix
            {
                public EntityAlive _ea;
                public EntityPlayer _playerTouched;
                public IEnumerator __result;
            }

            public static bool Prefix(EntityAlive _ea, EntityPlayer _playerTouched, ref IEnumerator __result)
            {
                var time = Time.time;
                if (VisibleScriptedSleepers.PlayerId == _playerTouched.entityId && time - VisibleScriptedSleepers.TouchTime < 2f)
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
                            weakBlock.OnEntityCollidedWithBlock(entity.world, 0, entity.blockPosStandingOn, entity.blockValueStandingOn, entity);
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
        /// Spawn unloaded sleepers.
        /// </summary>
        public static class SleeperVolume_Tick
        {
            private static DelayStorage<Vector3> Delays = new(2f);

            public struct APrefix
            {
                public SleeperVolume __instance;
                public World _world;
            }

            public static void Prefix(SleeperVolume __instance, World _world)
            {
                if (!__instance.isSpawning && !__instance.wasCleared && __instance.spawnsAvailable?.Count > 0)
                {
                    if (Delays.Check(__instance.Center))
                    {
                        EntityPlayer player;
                        if (Helper.AnyPlayerIsInRadius(_world, __instance.Center, VisibleScriptedSleepers.TouchDistance, out player) ||
                            Helper.AnyPlayerCanSeePos(_world, __instance.Center, 300f, out player))
                        {
                            __instance.UpdatePlayerTouched(_world, player);
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
                    if (Helper.AnyPlayerCanSeePos(__instance.theEntity.world, __instance.theEntity.position, 300f, out _) ||
                        Helper.AnyPlayerCanSeePos(__instance.theEntity.world, __instance.theEntity.getHeadPosition(), 300f, out _))
                    {
                        __instance.theEntity?.SetSleeperActive();
                    }
                }
            }
        }
    }
}
