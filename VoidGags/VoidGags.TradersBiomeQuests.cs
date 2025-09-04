using HarmonyLib;
using UnityEngine;
using static VoidGags.VoidGags.TradersBiomeQuests;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_TradersBiomeQuests()
        {
            LogApplyingPatch(nameof(Settings.TradersBiomeQuests));

            Harmony.Patch(AccessTools.Method(typeof(Quest), nameof(Quest.SetupPosition)),
                postfix: new HarmonyMethod(Quest_SetupPosition.Postfix));
        }

        public static class TradersBiomeQuests
        {
            /// <summary>
            /// Traders offer quests located in their biome only.
            /// </summary>
            public static class Quest_SetupPosition
            {
                public static void Postfix(Quest __instance, ref bool __result)
                {
                    if (!__result) return;
                    var caller = Helper.GetCallerMethod();
                    if (caller.DeclaringType == typeof(EntityTrader) && caller.Name == nameof(EntityTrader.PopulateActiveQuests))
                    {
                        var quest = __instance;
                        if (quest.QuestClass.Shareable && quest.PositionData.TryGetValue(Quest.PositionDataTypes.TraderPosition, out Vector3 traderPos))
                        {
                            var questBiome = GameManager.Instance.World.GetBiomeInWorld((int)quest.Position.x, (int)quest.Position.z);
                            var traderBiome = GameManager.Instance.World.GetBiomeInWorld((int)traderPos.x, (int)traderPos.z);
                            if (questBiome.m_Id != traderBiome.m_Id)
                            {
                                //LogModWarning($"Biome mismatch: [Trader:{traderBiome.LocalizedName}], [Quest:{questBiome.LocalizedName}] ({quest.QuestClass?.Name}) Distance: {traderPos.DistanceTo(quest.position):0}");
                                __result = false;
                            }
                        }
                    }
                }
            }
        }
    }
}
