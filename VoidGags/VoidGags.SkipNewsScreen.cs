using System.Collections.Generic;
using HarmonyLib;
using static NewsManager;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_SkipNewsScreen(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(XUiC_MainMenu), "Init"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => XUiC_MainMenu_Init.Prefix())));

            harmony.Patch(AccessTools.Method(typeof(NewsManager), "GetNewsData"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((List<NewsEntry> _target) => NewsManager_GetNewsData.Prefix(_target))));

            LogPatchApplied(nameof(Settings.SkipNewsScreen));
        }

        /// <summary>
        /// Skip News screen.
        /// </summary>
        public class XUiC_MainMenu_Init
        {
            public static void Prefix()
            {
                XUiC_MainMenu.shownNewsScreenOnce = true;
            }
        }

        /// <summary>
        /// Suppress GetNewsData() method.
        /// </summary>
        public class NewsManager_GetNewsData
        {
            public static bool Prefix(List<NewsEntry> _target)
            {
                _target.Clear();
                return false;
            }
        }
    }
}
