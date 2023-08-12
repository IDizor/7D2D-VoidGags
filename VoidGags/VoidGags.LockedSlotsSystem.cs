using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Platform;
using UniLinq;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_LockedSlotsSystem(Harmony harmony)
        {
            if (IsUndeadLegacy)
            {
                LogModWarning($"Patch '{nameof(Settings.LockedSlotsSystem)}' is not compatible with Undead Legacy.");
                return;
            }

            UseXmlPatches(nameof(Settings.LockedSlotsSystem));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), "ParseAttribute"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStack_ParseAttribute.APrefix p) =>
                XUiC_ItemStack_ParseAttribute.Prefix(ref p.__result, p._name, p._value))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStackGrid), "OnOpen"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStackGrid __instance) => XUiC_ItemStackGrid_OnOpen.Prefix(__instance))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ContainerStandardControls), "ChangeLockedSlots"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ContainerStandardControls_ChangeLockedSlots.APostfix p) =>
                XUiC_ContainerStandardControls_ChangeLockedSlots.Postfix(p.__instance, p._newValue))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ContainerStandardControls), "Init"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ContainerStandardControls __instance) => XUiC_ContainerStandardControls_Init.Postfix(__instance))));

            harmony.Patch(AccessTools.Method(typeof(World), "SetBlockRPC", new Type[] { typeof(int), typeof(Vector3i), typeof(BlockValue) }),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((World_SetBlockRPC.APrefix p) => World_SetBlockRPC.Prefix(p._blockPos, p._blockValue))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_LootWindow), "GetBindingValue"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_LootWindow_GetBindingValue.APrefix p) =>
                XUiC_LootWindow_GetBindingValue.Prefix(p.__instance, ref p._value, p._bindingName, ref p.__result))));

            harmony.Patch(AccessTools.Method(typeof(GameManager), "TEAccessClient"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vector3i _blockPos) => GameManager_TEAccessClient.Prefix(_blockPos))));

            harmony.Patch(AccessTools.Method(typeof(GameManager), "TEDeniedAccessClient"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vector3i _blockPos) => GameManager_TEDeniedAccessClient.Prefix(_blockPos))));

            OnGameLoadedActions.Add(XUiC_ContainerStandardControls_Init.LoadIgnoredContainers);

            LogPatchApplied(nameof(Settings.LockedSlotsSystem));

            if (Settings.AutoSpreadLootRadius < 2f || Settings.AutoSpreadLootRadius > 50f)
            {
                var v = Settings.AutoSpreadLootRadius;
                Settings.AutoSpreadLootRadius = Settings.AutoSpreadLootRadius < 2f ? 2f : 50f;
                LogModWarning($"Setting '{nameof(Settings.AutoSpreadLootRadius)}' value {v:0.0} is out of range (2..50). Value {Settings.AutoSpreadLootRadius:0} will be used instead.");
            }
        }

        private static string LockedSlotsSavesDirectory => FeaturesFolder + $"\\{nameof(Settings.LockedSlotsSystem)}\\Saves";
        public static List<Vector3i> IgnoredContainers = new List<Vector3i>();

        /// <summary>
        /// Reads colors for slots.
        /// </summary>
        public class XUiC_ItemStack_ParseAttribute
        {
            public static Color32 RegularColor;
            public static Color32 LockColor = new Color32(100, 20, 20, 255);

            public struct APrefix
            {
                public bool __result;
                public string _name;
                public string _value;
            }

            public static bool Prefix(ref bool __result, string _name, string _value)
            {
                if (_name == "locked_stack_color")
                {
                    LockColor = StringParsers.ParseColor32(_value);
                    __result = true;
                    return false;
                }

                if (_name == "background_color")
                {
                    RegularColor = StringParsers.ParseColor32(_value);
                }

                return true;
            }
        }

        /// <summary>
        /// Updates locked slots count on UI for opened backpack/vehicle.
        /// </summary>
        public class XUiC_ItemStackGrid_OnOpen
        {
            private static Dictionary<string, long> entityLockedSlots = new Dictionary<string, long>();

            public static void Prefix(XUiC_ItemStackGrid __instance)
            {
                XUiController parentController = null;
                string entityId = null;

                if (__instance is XUiC_Backpack)
                {
                    parentController = __instance.GetParentByType<XUiC_BackpackWindow>();
                    entityId = "p" + Helper.PlayerId;
                }
                else if (__instance is XUiC_VehicleContainer vehicleContainer)
                {
                    if (vehicleContainer.xui.vehicle != null)
                    {
                        parentController = __instance.GetParentByType<XUiC_VehicleStorageWindowGroup>();
                        entityId = "v" + vehicleContainer.xui.vehicle.entityId.ToString();
                    }
                }
                else if (__instance is XUiC_LootContainer)
                {
                    parentController = __instance.GetParentByType<XUiC_LootWindow>();
                    if (__instance.xui.lootContainer?.lootListName == "roboticDrone")
                    {
                        entityId = "roboticDrone";
                    }
                    else
                    {
                        // other loot containers should display no locked slots
                        var controls = parentController?.GetChildByType<XUiC_ContainerStandardControls>();
                        controls.ChangeLockedSlots(0);
                    }
                }

                if (entityId != null && parentController != null)
                {
                    var lockedSlots = LoadLockedSlotsCount(entityId);
                    var controls = parentController.GetChildByType<XUiC_ContainerStandardControls>();
                    if (controls != null)
                    {
                        // update controls
                        controls.ChangeLockedSlots(lockedSlots);

                        // update combobox on UI
                        var cbx = parentController.GetChildById("cbxLockedSlots") as XUiC_ComboBoxInt;
                        if (cbx != null)
                        {
                            cbx.Value = lockedSlots;
                        }
                    }
                }
            }

            public static void SaveLockedSlotsCount(long lockedSlots, string entityId)
            {
                //Debug.LogWarning($"SaveLockedSlotsCount({lockedSlots}, '{entityId}')");
                if (!string.IsNullOrEmpty(entityId))
                {
                    if (entityLockedSlots.ContainsKey(entityId) && entityLockedSlots[entityId] == lockedSlots)
                    {
                        return;
                    }
                    entityLockedSlots[entityId] = lockedSlots;
                    var worldSeed = Helper.WorldSeed;
                    if (!string.IsNullOrEmpty(worldSeed))
                    {
                        Directory.CreateDirectory(LockedSlotsSavesDirectory);
                        File.WriteAllText(Path.Combine(LockedSlotsSavesDirectory, $"{entityId}-{worldSeed}.txt"), lockedSlots.ToString());
                    }
                }
            }

            public static long LoadLockedSlotsCount(string entityId)
            {
                if (!string.IsNullOrEmpty(entityId))
                {
                    if (entityLockedSlots.ContainsKey(entityId))
                    {
                        return entityLockedSlots[entityId];
                    }
                    var worldSeed = Helper.WorldSeed;
                    if (!string.IsNullOrEmpty(worldSeed))
                    {
                        var configFile = Path.Combine(LockedSlotsSavesDirectory, $"{entityId}-{worldSeed}.txt");
                        if (File.Exists(configFile))
                        {
                            entityLockedSlots[entityId] = long.Parse(File.ReadAllText(configFile));
                            return entityLockedSlots[entityId];
                        }
                        entityLockedSlots[entityId] = 0;
                    }
                }
                return 0;
            }
        }

        /// <summary>
        /// Saves locked slots count and updates highlightning.
        /// </summary>
        public class XUiC_ContainerStandardControls_ChangeLockedSlots
        {
            static FieldInfo backgroundColor = AccessTools.Field(typeof(XUiC_ItemStack), "backgroundColor");

            public struct APostfix
            {
                public XUiC_ContainerStandardControls __instance;
                public long _newValue;
            }

            public static void Postfix(XUiC_ContainerStandardControls __instance, long _newValue)
            {
                var isDrone = __instance.xui.lootContainer?.lootListName == "roboticDrone" && __instance.GetParentByType<XUiC_LootWindow>() != null;
                var isVehicle = __instance.xui.vehicle != null && __instance.GetParentByType<XUiC_VehicleStorageWindowGroup>() != null;
                var isPlayerBackpack = __instance.GetParentByType<XUiC_BackpackWindow>() != null;
                //Debug.LogWarning($"LockedSlots changed: {_newValue}, isDrone = {isDrone}, isVehicle = {isVehicle}, isPlayerBackpack = {isPlayerBackpack}, lootListName = '{__instance.xui.lootContainer?.lootListName}'");
                var entityId = isDrone ? "roboticDrone"
                    : isPlayerBackpack ? "p" + Helper.PlayerId
                    : isVehicle ? "v" + __instance.xui.vehicle.entityId.ToString()
                    : null;

                if (entityId != null)
                {
                    XUiC_ItemStackGrid_OnOpen.SaveLockedSlotsCount((int)_newValue, entityId);
                }

                var stackGrid = __instance.GetItemStackGrid();
                if (stackGrid != null)
                {
                    var itemControllers = stackGrid.GetItemStackControllers();
                    for (int i = 0; i < itemControllers.Length; i++)
                    {
                        backgroundColor.SetValue(itemControllers[i], i < _newValue
                            ? XUiC_ItemStack_ParseAttribute.LockColor
                            : XUiC_ItemStack_ParseAttribute.RegularColor);
                        itemControllers[i].IsDirty = true; // set dirty to update UI
                    }
                }
            }
        }

        /// <summary>
        /// Auto-spreads the loot to nearby containers.
        /// </summary>
        public class XUiC_ContainerStandardControls_Init
        {
            public static bool Active = false;
            public static Vector3i? CurrentContainerPos = null;
            public static Vector3i? LocalOpenEntityPos = null;
            static FieldInfo stashLockedSlots = AccessTools.Field(typeof(XUiC_ContainerStandardControls), "stashLockedSlots");
            static MethodInfo closeInventoryLater = AccessTools.Method(typeof(XUiC_LootWindow), "closeInventoryLater");

            public static void Postfix(XUiC_ContainerStandardControls __instance)
            {
                // auto-spread button
                XUiController btnSpreadLoot = __instance.GetChildById("btnSpreadLoot");
                if (btnSpreadLoot != null)
                {
                    btnSpreadLoot.OnPress += (sender, _) => GameManager.Instance.StartCoroutine(SpreadLoot(sender));
                }

                IEnumerator SpreadLoot(XUiController sender)
                {
                    if (!Active)
                    {
                        Active = true;
                        var controls = sender.GetParentByType<XUiC_ContainerStandardControls>();
                        var localOpenContainer = controls.xui.lootContainer;
                        var isWorkstation = localOpenContainer == null && controls.xui.currentWorkstation?.Length > 0;
                        var workstation = isWorkstation ? GetWorkstation(controls.xui.currentWorkstation) : null;
                        LocalOpenEntityPos = isWorkstation ? workstation?.ToWorldPos() : localOpenContainer?.ToWorldPos();
                        var localOpenEntityId = isWorkstation ? workstation?.entityId : localOpenContainer?.EntityId;

                        var loot = controls.GetItemStackGrid();
                        var lockedSlots = (int)stashLockedSlots.GetValue(controls);
                        var startFromBottom = controls.MoveStartBottomRight;

                        if (LocalOpenEntityPos.HasValue)
                        {
                            if (localOpenContainer != null && !IgnoredContainers.Contains(LocalOpenEntityPos.Value))
                            {
                                XUiM_LootContainer.StashItems(loot, localOpenContainer, lockedSlots, XUiM_LootContainer.EItemMoveKind.FillAndCreate, startFromBottom);
                            }
                            if (!IsServer)
                            {
                                // unlock container while spreading to be able to lock other containers
                                GameManager.Instance.TEUnlockServer(0, LocalOpenEntityPos.Value, localOpenEntityId.Value, _allowContainerDestroy: false);
                            }
                        }

                        var player = Helper.PlayerLocal;
                        var tiles = Helper.GetTileEntities(player.position, Settings.AutoSpreadLootRadius);
                        var timeLimit = Time.time + 5f;

                        foreach (var tile in tiles)
                        {
                            var tilePos = tile.ToWorldPos();
                            if (!IgnoredContainers.Contains(tilePos) && tilePos != LocalOpenEntityPos && tile is TileEntityLootContainer box)
                            {
                                if (box.bTouched)
                                {
                                    var secureBox = box as TileEntitySecureLootContainer;
                                    var hasAccess = secureBox == null || !secureBox.IsLocked() || secureBox.IsUserAllowed(PlatformManager.InternalLocalUserIdentifier);
                                    if (hasAccess)
                                    {
                                        var boxPos = tilePos;
                                        var deniedPos = boxPos.Invert();
                                        var openerEntityId = GameManager.Instance.GetEntityIDForLockedTileEntity(box);
                                        var isOpenByAnyPlayer = openerEntityId > 0; // this variable can be used on server only
                                        if (IsServer)
                                        {
                                            if (!isOpenByAnyPlayer)
                                            {
                                                Spread();
                                            }
                                            else if (LocalPlayerUI.GetUIForPlayer(player) != null && box.ContainsAnyItem(loot, lockedSlots))
                                            {
                                                GameManager.ShowTooltip(player, Localization.Get("ttNoInteractItem"), string.Empty, "ui_denied");
                                            }
                                        }
                                        else if (box.ContainsAnyItem(loot, lockedSlots))
                                        {
                                            // send "TELockServer" and wait until response processed in "TEAccessClient" and then call Spread()
                                            CurrentContainerPos = boxPos;
                                            GameManager.Instance.TELockServer(0, boxPos, box.entityId, player.entityId, null);

                                            var timedOut = false;
                                            var waitFlag = true;
                                            while (waitFlag)
                                            {
                                                yield return new WaitForSeconds(0.05f);
                                                var denied = CurrentContainerPos == deniedPos;
                                                var access = CurrentContainerPos == null;
                                                timedOut = Time.time > timeLimit;
                                                waitFlag = !denied && !access && !timedOut;
                                                if (access)
                                                {
                                                    Spread();
                                                    LeaveBox();
                                                }
                                            }

                                            if (timedOut)
                                            {
                                                CloseInventory();
                                                LeaveBox();
                                                LocalOpenEntityPos = null;
                                            }
                                        }

                                        void Spread()
                                        {
                                            XUiM_LootContainer.StashItems(loot, box, lockedSlots, XUiM_LootContainer.EItemMoveKind.FillAndCreate, startFromBottom);
                                        }

                                        void LeaveBox()
                                        {
                                            GameManager.Instance.TEUnlockServer(0, boxPos, box.entityId, _allowContainerDestroy: false);
                                        }
                                    }
                                }
                            }
                        }
                        Reset();

                        if (!IsServer && LocalOpenEntityPos.HasValue)
                        {
                            GameManager.Instance.TELockServer(0, LocalOpenEntityPos.Value, localOpenEntityId.Value, player.entityId, null);
                        }
                    }
                }
                
                // ignore container buttons
                XUiController btnSpreadReceiver = __instance.GetChildById("btnSpreadReceiver");
                XUiController btnSpreadIgnorer = __instance.GetChildById("btnSpreadIgnorer");
                if (btnSpreadReceiver != null) btnSpreadReceiver.OnPress += ToggleReceiver;
                if (btnSpreadIgnorer != null) btnSpreadIgnorer.OnPress += ToggleReceiver;

                void ToggleReceiver(XUiController sender, int _)
                {
                    var window = sender.xui.GetWindowByType<XUiC_LootWindow>();
                    var lootContainer = sender.xui.lootContainer;
                    if (window != null && lootContainer != null)
                    {
                        var pos = lootContainer.ToWorldPos();
                        if (!IgnoredContainers.Remove(pos))
                        {
                            IgnoredContainers.Add(pos);
                        }
                        window.RefreshBindings();
                        SaveIgnoredContainers();
                    }
                }
            }

            // auxiliary methods

            public static void CloseInventory()
            {
                var playerUI = LocalPlayerUI.GetUIForPlayer(Helper.PlayerLocal);
                var lootWindow = playerUI?.xui.GetChildByType<XUiC_LootWindow>();
                if (lootWindow != null)
                {
                    ThreadManager.StartCoroutine((IEnumerator)closeInventoryLater.Invoke(lootWindow, null));
                }
            }

            public static void SaveIgnoredContainers()
            {
                var playerId = Helper.PlayerId;
                var worldSeed = Helper.WorldSeed;
                Directory.CreateDirectory(LockedSlotsSavesDirectory);
                var filePath = Path.Combine(LockedSlotsSavesDirectory, $"auto-spread-ignore-p{playerId}-{worldSeed}.txt");
                File.WriteAllLines(filePath, IgnoredContainers.Select(pos => pos.ToString()));
            }

            public static void LoadIgnoredContainers()
            {
                IgnoredContainers.Clear();
                var playerId = Helper.PlayerId;
                var worldSeed = Helper.WorldSeed;
                var filePath = Path.Combine(LockedSlotsSavesDirectory, $"auto-spread-ignore-p{playerId}-{worldSeed}.txt");
                if (File.Exists(filePath))
                {
                    foreach (var line in File.ReadAllLines(filePath))
                    {
                        var pos = Vector3i.Parse(line);
                        if (pos != Vector3i.zero)
                        {
                            IgnoredContainers.Add(pos);
                        }
                    }
                }
            }

            public static TileEntityWorkstation GetWorkstation(string workstationName)
            {
                var playerUI = LocalPlayerUI.GetUIForPlayer(Helper.PlayerLocal);
                var windows = playerUI?.xui.GetWindowsByType<XUiC_WorkstationWindowGroup>();
                return windows?.FirstOrDefault(w => w.Workstation == workstationName)?.WorkstationData?.TileEntity;
            }

            public static void Reset()
            {
                CurrentContainerPos = null;
                Active = false;
            }
        }

        /// <summary>
        /// Removes block from the ignored containers when destroyed.
        /// </summary>
        public class World_SetBlockRPC
        {
            public struct APrefix
            {
                public Vector3i _blockPos;
                public BlockValue _blockValue;
            }

            public static void Prefix(Vector3i _blockPos, BlockValue _blockValue)
            {
                if (_blockValue.isair || GameManager.Instance.World.GetBlock(_blockPos).isair)
                {
                    if (IgnoredContainers.Remove(_blockPos))
                    {
                        XUiC_ContainerStandardControls_Init.SaveIgnoredContainers();
                    }
                }
            }
        }

        /// <summary>
        /// Prepares custom binding values for loot containers.
        /// </summary>
        public class XUiC_LootWindow_GetBindingValue
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
                if (_bindingName == "auto_spread_ignore")
                {
                    _value = "false";
                    var containerPos = __instance.xui.lootContainer?.ToWorldPos();
                    if (containerPos.HasValue)
                    {
                        _value = IgnoredContainers.Contains(containerPos.Value).ToString();
                    }
                    __result = true;
                    return false;
                }
                if (_bindingName == "is_container_block")
                {
                    _value = (__instance.xui.lootContainer != null && __instance.xui.lootContainer.GetChunk() != null).ToString();
                    __result = true;
                    return false;
                }
                if (_bindingName == "is_drone")
                {
                    _value = (__instance.xui.lootContainer != null && __instance.xui.lootContainer.GetChunk() == null && __instance.xui.lootContainer.lootListName == "roboticDrone").ToString();
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Processes the success response from TEUnlockServer() when spreading the loot.
        /// </summary>
        public class GameManager_TEAccessClient
        {
            public static bool Prefix(Vector3i _blockPos)
            {
                if (!IsServer)
                {
                    if (XUiC_ContainerStandardControls_Init.CurrentContainerPos == _blockPos)
                    {
                        XUiC_ContainerStandardControls_Init.CurrentContainerPos = null;
                        return false;
                    }
                    else if (XUiC_ContainerStandardControls_Init.LocalOpenEntityPos == _blockPos)
                    {
                        XUiC_ContainerStandardControls_Init.LocalOpenEntityPos = null;
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Processes the denied response from TEUnlockServer() when spreading the loot.
        /// </summary>
        public class GameManager_TEDeniedAccessClient
        {
            public static void Prefix(Vector3i _blockPos)
            {
                if (!IsServer)
                {
                    if (XUiC_ContainerStandardControls_Init.CurrentContainerPos == _blockPos)
                    {
                        XUiC_ContainerStandardControls_Init.CurrentContainerPos = _blockPos.Invert();
                    }
                    else if (XUiC_ContainerStandardControls_Init.LocalOpenEntityPos == _blockPos)
                    {
                        XUiC_ContainerStandardControls_Init.LocalOpenEntityPos = null;
                        XUiC_ContainerStandardControls_Init.CloseInventory();
                    }
                }
            }
        }
    }
}
