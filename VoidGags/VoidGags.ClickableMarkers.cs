using HarmonyLib;
using UniLinq;
using static VoidGags.VoidGags.ClickableMarkers;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ClickableMarkers()
        {
            LogApplyingPatch(nameof(Settings.ClickableMarkers));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_MapArea), nameof(XUiC_MapArea.onMapPressedLeft)),
                prefix: new HarmonyMethod(XUiC_MapArea_onMapPressedLeft.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_MapArea), nameof(XUiC_MapArea.onMapPressed)),
                prefix: new HarmonyMethod(XUiC_MapArea_onMapPressed.Prefix));
        }

        public static class ClickableMarkers
        {
            public static bool SkipLeftClickPatch = false;

            /// <summary>
            /// Click on the map marker selects the waypoint in the list.
            /// </summary>
            public static class XUiC_MapArea_onMapPressedLeft // lift click
            {
                public static bool Prefix(XUiC_MapArea __instance)
                {
                    if (SkipLeftClickPatch)
                    {
                        SkipLeftClickPatch = false;
                        return true;
                    }
                    if (__instance.closestMouseOverNavObject != null)
                    {
                        var player = __instance.xui.playerUI.entityPlayer;
                        var quest = player.QuestJournal.quests.FirstOrDefault(q => q.NavObject == __instance.closestMouseOverNavObject);
                        // if quest waypoint
                        if (quest != null)
                        {
                            // open quests window and select quest
                            XUiC_WindowSelector.OpenSelectorAndWindow(player, "quests");
                            var questsWindow = player.playerUI.xui.GetChildByType<XUiC_QuestListWindow>();
                            var questEntry = questsWindow?.questList?.entryList.FirstOrDefault(e => e.quest == quest);
                            if (questEntry != null)
                            {
                                questsWindow.questList.OnPressQuest(questEntry, 1);
                            }
                        }
                        else
                        {
                            // select waypoint in the list
                            var waypointList = __instance.xui.GetChildByType<XUiC_MapWaypointList>();
                            waypointList?.SelectWaypoint(__instance.closestMouseOverNavObject);
                        }
                        return false;
                    }
                    return true;
                }
            }

            /// <summary>
            /// Swap left click and right click functions for markers.
            /// </summary>
            public static class XUiC_MapArea_onMapPressed // right click
            {
                public static bool Prefix(XUiC_MapArea __instance, XUiController _sender, int _mouseButton)
                {
                    if (__instance.closestMouseOverNavObject != null && !InputUtils.ShiftKeyPressed)
                    {
                        SkipLeftClickPatch = true;
                        __instance.onMapPressedLeft(_sender, _mouseButton);
                        return false;
                    }
                    return true;
                }
            }
        }
    }
}
