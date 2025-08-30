using System.Collections.Generic;
using HarmonyLib;
using static NewsManager;
using static VoidGags.VoidGags.SkipNewsScreen;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_SkipNewsScreen()
        {
            LogApplyingPatch(nameof(Settings.SkipNewsScreen));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_MainMenu), nameof(XUiC_MainMenu.Init)),
                prefix: new HarmonyMethod(XUiC_MainMenu_Init.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(NewsManager), nameof(NewsManager.GetNewsData)),
                prefix: new HarmonyMethod(NewsManager_GetNewsData.Prefix));
        }

        public static class SkipNewsScreen
        {
            /// <summary>
            /// Skip News screen.
            /// </summary>
            public static class XUiC_MainMenu_Init
            {
                public static void Prefix()
                {
                    XUiC_MainMenu.shownNewsScreenOnce = true;
                }
            }

            /// <summary>
            /// Suppress GetNewsData() method.
            /// </summary>
            public static class NewsManager_GetNewsData
            {
                public static bool Prefix(List<NewsEntry> _target)
                {
                    _target.Clear();
                    return false;
                }
            }
        }
    }
}
