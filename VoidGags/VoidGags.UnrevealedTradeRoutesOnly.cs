using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_UnrevealedTradeRoutesOnly(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ObjectiveGoto), "GetPosition"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((EntityPlayer entityPlayer) => ObjectiveGoto_GetPosition.Prefix(entityPlayer))),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => ObjectiveGoto_GetPosition.Postfix())));

            harmony.Patch(AccessTools.Method(typeof(QuestJournal), "GetTraderList"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((List<Vector2> __result) => QuestJournal_GetTraderList.Postfix(ref __result))));

            LogPatchApplied(nameof(Settings.UnrevealedTradeRoutesOnly));
        }

        /// <summary>
        /// Tracks the player who can take quest to open new trade routes.
        /// </summary>
        public class ObjectiveGoto_GetPosition
        {
            public static EntityPlayer Player = null;

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
        public class QuestJournal_GetTraderList
        {
            public static void Postfix(ref List<Vector2> __result)
            {
                if (ObjectiveGoto_GetPosition.Player != null)
                {
                    if (__result == null)
                    {
                        __result = new List<Vector2>();
                    }
                    var waypoints = ObjectiveGoto_GetPosition.Player.Waypoints?.List;
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
