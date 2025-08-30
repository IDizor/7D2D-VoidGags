using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using static VoidGags.VoidGags.PreventDestroyOnClose;

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
                prefix: new HarmonyMethod(GameManager_TEUnlockServer.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_LootWindow), nameof(XUiC_LootWindow.Init)),
                postfix: new HarmonyMethod(XUiC_LootWindow_Init.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiController), nameof(XUiController.GetBindingValue)),
                prefix: new HarmonyMethod(XUiController_GetBindingValue.Prefix));

            OnGameLoadedActions.Add(LoadPreventDestroyValues);
        }

        public static class PreventDestroyOnClose
        {
            public static List<string> Containers = [];
            public static string PreventDestroySaveFile => FeaturesFolderPath + $"\\{nameof(Settings.PreventDestroyOnClose)}\\save.txt";

            public static void UpdateAndSavePreventDestroyValue(string containerName, bool destroy)
            {
                if (!string.IsNullOrEmpty(containerName))
                {
                    if (destroy && Containers.Contains(containerName))
                    {
                        Containers.Remove(containerName);
                    }
                    else if (!destroy && !Containers.Contains(containerName))
                    {
                        Containers.Add(containerName);
                    }

                    File.WriteAllLines(PreventDestroySaveFile, Containers);
                }
            }

            public static void LoadPreventDestroyValues()
            {
                if (File.Exists(PreventDestroySaveFile))
                {
                    Containers = new(File.ReadAllLines(PreventDestroySaveFile));
                }
            }

            /// <summary>
            /// Prevent the loot container from being auto-destroyed.
            /// </summary>
            public static class GameManager_TEUnlockServer
            {
                public static void Prefix(Vector3i _blockPos, ref bool _allowContainerDestroy)
                {
                    if (_allowContainerDestroy)
                    {
                        var tileEntity = GameManager.Instance.World.GetTileEntity(_blockPos);
                        var blockName = tileEntity?.blockValue.Block?.blockName;

                        if (!string.IsNullOrEmpty(blockName))
                        {
                            _allowContainerDestroy = !Containers.Contains(blockName);
                        }
                    }
                }
            }

            /// <summary>
            /// Assign actions for loot containers UI buttons.
            /// </summary>
            public static class XUiC_LootWindow_Init
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
                        var destroy = Containers.Contains(blockName);
                        UpdateAndSavePreventDestroyValue(blockName, destroy);
                        window?.RefreshBindings();
                    }
                }
            }

            /// <summary>
            /// Custom binding values for loot containers.
            /// </summary>
            public static class XUiController_GetBindingValue
            {
                public static bool Prefix(XUiController __instance, ref string _value, string _bindingName, ref bool __result)
                {
                    if (__instance is XUiC_LootWindow)
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
                                    _value = (Containers.Contains(blockName) == true).ToString();
                                }
                            }
                            __result = true;
                            return false;
                        }
                    }
                    return true;
                }
            }
        }
    }
}
