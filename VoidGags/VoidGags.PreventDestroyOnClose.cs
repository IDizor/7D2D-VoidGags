using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PreventDestroyOnClose(Harmony harmony)
        {
            if (Settings.PreventDestroyOnClose_KeyCode > 0)
            {
                GameManager_TEUnlockServer.PreventAutoDestroyKey = (KeyCode)Settings.PreventDestroyOnClose_KeyCode;
            }

            harmony.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.TEUnlockServer)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((bool _allowContainerDestroy) => GameManager_TEUnlockServer.Prefix(ref _allowContainerDestroy))));

            LogPatchApplied(nameof(Settings.PreventDestroyOnClose));
        }

        /// <summary>
        /// Hold the Left Shift to prevent the loot container from being auto-destroyed once closed.
        /// </summary>
        public class GameManager_TEUnlockServer
        {
            public static KeyCode PreventAutoDestroyKey = KeyCode.LeftShift;
            
            public static void Prefix(ref bool _allowContainerDestroy)
            {
                if (Input.GetKey(PreventAutoDestroyKey))
                {
                    _allowContainerDestroy = false;
                }
            }
        }
    }
}
