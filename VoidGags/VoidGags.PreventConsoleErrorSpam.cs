using HarmonyLib;
using static VoidGags.VoidGags.PreventConsoleErrorSpam;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PreventConsoleErrorSpam()
        {
            LogApplyingPatch(nameof(Settings.PreventConsoleErrorSpam));

            Harmony.Patch(AccessTools.Method(typeof(GUIWindowConsole), nameof(GUIWindowConsole.openConsole)),
                prefix: new HarmonyMethod(GUIWindowConsole_openConsole.Prefix));
        }

        public static class PreventConsoleErrorSpam
        {
            public static bool WasOpen = false;

            /// <summary>
            /// Allow to auto-open console only for the first exception to avoid losing player control in case of exceptions spam.
            /// </summary>
            public static class GUIWindowConsole_openConsole
            {
                public static bool Prefix()
                {
                    if (!WasOpen)
                    {
                        WasOpen = true;
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
