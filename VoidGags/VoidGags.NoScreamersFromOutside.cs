using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static NetPackageQuestEvent;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_NoScreamersFromOutside()
        {
            LogApplyingPatch(nameof(Settings.NoScreamersFromOutside));

            Harmony.Patch(AccessTools.Method(typeof(AIDirectorChunkEventComponent), nameof(AIDirectorChunkEventComponent.SpawnScouts)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vector3 targetPos) => AIDirectorChunkEventComponent_SpawnScouts.Prefix(targetPos))));

            Harmony.Patch(AccessTools.Method(typeof(NetPackageQuestEvent), nameof(NetPackageQuestEvent.ProcessPackage)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((NetPackageQuestEvent __instance) => NetPackageQuestEvent_ProcessPackage.Postfix(__instance))));
        }

        /// <summary>
        /// Prevents zombie Screamers from spawning near players with an active quest.
        /// </summary>
        public class AIDirectorChunkEventComponent_SpawnScouts
        {
            const float spawnRadius = 150;

            public static bool Prefix(Vector3 targetPos)
            {
                if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                {
                    // check active quests from clients
                    var activeQuests = NetPackageQuestEvent_ProcessPackage.ActiveQuests;
                    if (activeQuests.Any(q => (targetPos - q.PrefabCenter).magnitude < spawnRadius))
                    {
                        LogModWarning("Outside spawn of zombie Screamer is prevented. Player has an active quest.");
                        return false;
                    }

                    // check local player quest
                    if (!GameManager.IsDedicatedServer)
                    {
                        foreach (var player in GameManager.Instance.World.Players.list)
                        {
                            if (player.QuestJournal.ActiveQuest?.CurrentState == Quest.QuestState.InProgress)
                            {
                                var pos = new Vector3(targetPos.x, player.position.y, targetPos.z);
                                if ((player.position - pos).magnitude < spawnRadius)
                                {
                                    LogModWarning("Outside spawn of zombie Screamer is prevented. Player has an active quest.");
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Tracks active quests on the server from the remote clients.
        /// </summary>
        public class NetPackageQuestEvent_ProcessPackage
        {
            public static List<ActiveQuestPrefab> ActiveQuests = [];

            public class ActiveQuestPrefab
            {
                public Vector3 PrefabPos;
                public Vector3i PrefabCenter;
                public float StartTime;
            }

            public static void Postfix(NetPackageQuestEvent __instance)
            {
                if (IsServer)
                {
                    if (__instance.eventType == QuestEventTypes.LockPOI)
                    {
                        var prefabDecorator = GameManager.Instance.World.ChunkClusters[0].ChunkProvider.GetDynamicPrefabDecorator();
                        var prefab = prefabDecorator.GetPrefabAtPosition(__instance.prefabPos);
                        if (prefab != null)
                        {
                            ActiveQuests.Add(new ActiveQuestPrefab
                            {
                                PrefabPos = __instance.prefabPos,
                                PrefabCenter = prefab.boundingBoxPosition + new Vector3i(prefab.boundingBoxSize.ToVector3() / 2),
                                StartTime = Time.time,
                            });
                            //Debug.LogWarning("Quest started!");
                        }
                    }
                    if (__instance.eventType == QuestEventTypes.ClearSleeper)
                    {
                        var now = Time.time;
                        var removed = ActiveQuests.RemoveAll(q => now - q.StartTime > 5 && __instance.prefabPos == q.PrefabPos);
                        if (removed > 0)
                        {
                            //Debug.LogWarning("Quest finished!");
                        }
                    }
                }
            }
        }
    }
}
