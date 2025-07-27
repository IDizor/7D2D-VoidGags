using System.Collections.Generic;
using System.IO;
using HarmonyLib;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_PreventDestroyOnClose()
        {
            LogApplyingPatch(nameof(Settings.PreventDestroyOnClose));
            UseXmlPatches(nameof(Settings.PreventDestroyOnClose));

            Harmony.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.TEUnlockServer)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((GameManager_TEUnlockServer.APrefix p) => GameManager_TEUnlockServer.Prefix(p._blockPos, ref p._allowContainerDestroy))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_LootWindow), nameof(XUiC_LootWindow.Init)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_LootWindow __instance) => XUiC_LootWindow_Init.Postfix(__instance))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_LootWindow), nameof(XUiC_LootWindow.GetBindingValue)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_LootWindow_GetBindingValue_2.APrefix p) => XUiC_LootWindow_GetBindingValue_2.Prefix(p.__instance, ref p._value, p._bindingName, ref p.__result))));

            OnGameLoadedActions.Add(LoadPreventDestroyValues);
        }

        public static class PreventDestroyOnClose
        {
            public static List<string> Containers = [];
            public static string PreventDestroySaveFile => FeaturesFolderPath + $"\\{nameof(Settings.PreventDestroyOnClose)}\\save.txt";
        }

        /// <summary>
        /// Prevent the loot container from being auto-destroyed.
        /// </summary>
        public class GameManager_TEUnlockServer
        {
            public struct APrefix
            {
                public Vector3i _blockPos;
                public bool _allowContainerDestroy;
            }

            public static void Prefix(Vector3i _blockPos, ref bool _allowContainerDestroy)
            {
                if (_allowContainerDestroy)
                {
                    var tileEntity = GameManager.Instance.World.GetTileEntity(_blockPos);
                    var blockName = tileEntity?.blockValue.Block?.blockName;

                    if (!string.IsNullOrEmpty(blockName))
                    {
                        _allowContainerDestroy = !PreventDestroyOnClose.Containers.Contains(blockName);
                    }
                }
            }
        }

        /// <summary>
        /// Assign actions for loot containers UI buttons.
        /// </summary>
        public class XUiC_LootWindow_Init
        {
            public static void Postfix(XUiC_LootWindow __instance)
            {
                var btnPreventAutoDestroy = __instance.GetChildById("btnPreventAutoDestroy");
                var btnAllowAutoDestroy = __instance.GetChildById("btnAllowAutoDestroy");
                if (btnPreventAutoDestroy != null) btnPreventAutoDestroy.OnPress += ToggleAutoDestroy;
                if (btnAllowAutoDestroy != null) btnAllowAutoDestroy.OnPress += ToggleAutoDestroy;

                static void ToggleAutoDestroy(XUiController _sender, int _mouseButton)
                {
                    var window = _sender.xui.GetWindowByType<XUiC_LootWindow>();
                    var blockName = _sender.xui.lootContainer?.blockValue.Block?.blockName;
                    var destroy = PreventDestroyOnClose.Containers.Contains(blockName);
                    UpdateAndSavePreventDestroyValue(blockName, destroy);
                    window?.RefreshBindings();
                }
            }
        }

        /// <summary>
        /// Custom binding values for loot containers.
        /// </summary>
        public class XUiC_LootWindow_GetBindingValue_2
        {
            public struct APrefix
            {
                public XUiC_LootWindow __instance;
                public string _value;
                public string _bindingName;
                public bool __result;
            }

            public static bool Prefix(XUiC_LootWindow __instance, ref string _value, string _bindingName, ref bool __result)
            {
                if (_bindingName == "is_auto_destroyable")
                {
                    _value = "false";
                    var lootableEntity = __instance.xui.lootContainer;
                    if (lootableEntity != null && lootableEntity.GetChunk() != null)
                    {
                        if (!string.IsNullOrEmpty(lootableEntity.lootListName))
                        {
                            var container = LootContainer.GetLootContainer(lootableEntity.lootListName);
                            if (container != null)
                                _value = (container.destroyOnClose != LootContainer.DestroyOnClose.False).ToString();
                        }
                    }
                    __result = true;
                    return false;
                }
                if (_bindingName == "prevent_auto_destroy")
                {
                    _value = "false";
                    var lootableEntity = __instance.xui.lootContainer;
                    if (lootableEntity != null && lootableEntity.GetChunk() != null)
                    {
                        var blockName = lootableEntity.blockValue.Block?.blockName;
                        if (!string.IsNullOrEmpty(blockName))
                        {
                            _value = (PreventDestroyOnClose.Containers.Contains(blockName) == true).ToString();
                        }
                    }
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        public static void UpdateAndSavePreventDestroyValue(string containerName, bool destroy)
        {
            if (!string.IsNullOrEmpty(containerName))
            {
                if (destroy && PreventDestroyOnClose.Containers.Contains(containerName))
                {
                    PreventDestroyOnClose.Containers.Remove(containerName);
                }
                else if (!destroy && !PreventDestroyOnClose.Containers.Contains(containerName))
                {
                    PreventDestroyOnClose.Containers.Add(containerName);
                }

                File.WriteAllLines(PreventDestroyOnClose.PreventDestroySaveFile, PreventDestroyOnClose.Containers);
            }
        }

        public static void LoadPreventDestroyValues()
        {
            if (File.Exists(PreventDestroyOnClose.PreventDestroySaveFile))
            {
                PreventDestroyOnClose.Containers = new(File.ReadAllLines(PreventDestroyOnClose.PreventDestroySaveFile));
            }
        }
    }
}
