using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.UnrevealedTradeRoutesOnly;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_UnrevealedTradeRoutesOnly()
        {
            LogApplyingPatch(nameof(Settings.UnrevealedTradeRoutesOnly));

            Harmony.Patch(AccessTools.Method(typeof(ObjectiveGoto), nameof(ObjectiveGoto.GetPosition)),
                prefix: new HarmonyMethod(ObjectiveGoto_GetPosition.Prefix),
                postfix: new HarmonyMethod(ObjectiveGoto_GetPosition.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(QuestJournal), nameof(QuestJournal.GetTraderList)),
                postfix: new HarmonyMethod(QuestJournal_GetTraderList.Postfix));
        }

        public static class UnrevealedTradeRoutesOnly
        {
            public static EntityPlayer Player = null;

            /// <summary>
            /// Tracks the player who can take quest to open new trade routes.
            /// </summary>
            public static class ObjectiveGoto_GetPosition
            {
                public static void Prefix(EntityPlayer entityPlayer)
                {
                    Player = entityPlayer;
                }

                public static void Postfix()
                {
                    Player = null;
                }
            }

            /// <summary>
            /// Adds revealed traders to the ignore list.
            /// </summary>
            public static class QuestJournal_GetTraderList
            {
                public static void Postfix(ref List<Vector2> __result)
                {
                    if (Player != null)
                    {
                        if (__result == null)
                        {
                            __result = [];
                        }
                        var waypoints = Player.Waypoints?.Collection.list;
                        if (waypoints != null)
                        {
                            var prefabDecorator = GameManager.Instance.World.ChunkClusters[0].ChunkProvider.GetDynamicPrefabDecorator();
                            foreach (var w in waypoints)
                            {
                                //Debug.LogWarning($"Waypoint: {w.icon}, {w.name}, {w.DisplayName}, {Helper.WorldPosToCompasText(w.pos)}");
                                var traderPrefab = prefabDecorator.GetClosestPOIToWorldPos(QuestEventManager.traderTag, new Vector2(w.pos.x, w.pos.z));
                                if (traderPrefab != null)
                                {
                                    var prefabCenter = traderPrefab.boundingBoxPosition + (traderPrefab.boundingBoxSize.ToVector3() / 2);
                                    var distanceToWaypoint = (new Vector3(w.pos.x, prefabCenter.y, w.pos.z) - prefabCenter).magnitude;
                                    //Debug.LogError($"Trader: {traderPrefab.name} (distance = {distanceToWaypoint:0.0}), {Helper.WorldPosToCompasText(traderPrefab.boundingBoxPosition)}");
                                    if (distanceToWaypoint < 50)
                                    {
                                        __result.Add(new Vector2(traderPrefab.boundingBoxPosition.x, traderPrefab.boundingBoxPosition.z));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
