using HarmonyLib;

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
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => GUIWindowConsole_openConsole.Prefix())));
        }

        /// <summary>
        /// Allow to auto-open console only for the first exception to avoid losing player control in case of exceptions spam.
        /// </summary>
        public class GUIWindowConsole_openConsole
        {
            static bool firstTime = true;

            public static bool Prefix()
            {
                if (firstTime)
                {
                    firstTime = false;
                    return true;
                }
                return false;
            }
        }
    }
}
