using System;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PreventConsoleErrorSpam(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(GUIWindowConsole), "openConsole"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => GUIWindowConsole_openConsole.Prefix())));

            LogPatchApplied(nameof(Settings.PreventConsoleErrorSpam));
        }

        /// <summary>
        /// Makes the scrapping process in inventory faster, depending on the Salvage Operations perk level.
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
