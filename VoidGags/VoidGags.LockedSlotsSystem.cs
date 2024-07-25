using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json;
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

            if (Settings.LockedSlotsSystem_AutoSpreadButton)
            {
                UseXmlPatches(nameof(Settings.LockedSlotsSystem_AutoSpreadButton));
            }

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.ParseAttribute)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStack_ParseAttribute.APrefix p) =>
                XUiC_ItemStack_ParseAttribute.Prefix(ref p.__result, p._name, p._value))));

            harmony.Patch(AccessTools.PropertyGetter(typeof(XUiC_ContainerStandardControls), nameof(XUiC_ContainerStandardControls.LockedSlots)), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ContainerStandardControls_LockedSlots_Getter.APostfix p) =>
                XUiC_ContainerStandardControls_LockedSlots_Getter.Postfix(p.__instance, ref p.__result))));

            harmony.Patch(AccessTools.PropertySetter(typeof(XUiC_ContainerStandardControls), nameof(XUiC_ContainerStandardControls.LockedSlots)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ContainerStandardControls_LockedSlots_Setter.APrefix p) =>
                XUiC_ContainerStandardControls_LockedSlots_Setter.Prefix(p.__instance, p.value))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStackGrid), nameof(XUiC_ItemStackGrid.OnOpen)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStackGrid __instance) => XUiC_ItemStackGrid_OnOpen.Prefix(__instance))));

            harmony.Patch(AccessTools.Method(typeof(XUiController), nameof(XUiController.RefreshBindings)), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiController __instance) => XUiController_RefreshBindings.Postfix(__instance))));

            harmony.Patch(AccessTools.Method(typeof(Bag), nameof(Bag.AddItem)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((Bag_AddItem.AParams p) => Bag_AddItem.Prefix(p.__instance, ref p.__state))),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((Bag_AddItem.AParams p) => Bag_AddItem.Postfix(p.__instance, p.__state))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ContainerStandardControls), nameof(XUiC_ContainerStandardControls.Init)), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ContainerStandardControls __instance) => XUiC_ContainerStandardControls_Init.Postfix(__instance))));

            harmony.Patch(AccessTools.Method(typeof(World), nameof(World.SetBlockRPC), new Type[] { typeof(int), typeof(Vector3i), typeof(BlockValue) }),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((World_SetBlockRPC.APrefix p) => World_SetBlockRPC.Prefix(p._blockPos, p._blockValue))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), nameof(XUiC_ItemStack.GetBindingValue)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStack_GetBindingValue.APrefix p) =>
                XUiC_ItemStack_GetBindingValue.Prefix(p.__instance, ref p._value, p._bindingName, ref p.__result))),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStack_GetBindingValue.APostfix p) =>
                XUiC_ItemStack_GetBindingValue.Postfix(p.__instance, ref p._value, p._bindingName))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStackGrid), nameof(XUiC_ItemStackGrid.OnClose)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStackGrid __instance) => XUiC_ItemStackGrid_OnClose.Prefix(__instance))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_LootWindow), nameof(XUiC_LootWindow.GetBindingValue)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_LootWindow_GetBindingValue.APrefix p) =>
                XUiC_LootWindow_GetBindingValue.Prefix(p.__instance, ref p._value, p._bindingName, ref p.__result))));

            harmony.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.TEAccessClient)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vector3i _blockPos) => GameManager_TEAccessClient.Prefix(_blockPos))));

            harmony.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.TEDeniedAccessClient)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vector3i _blockPos) => GameManager_TEDeniedAccessClient.Prefix(_blockPos))));

            OnGameLoadedActions.Add(XUiC_ContainerStandardControls_Init.LoadIgnoredContainers);

            LogPatchApplied(nameof(Settings.LockedSlotsSystem));

            if (Settings.LockedSlotsSystem_AutoSpreadRadius < 2f || Settings.LockedSlotsSystem_AutoSpreadRadius > 50f)
            {
                var v = Settings.LockedSlotsSystem_AutoSpreadRadius;
                Settings.LockedSlotsSystem_AutoSpreadRadius = Settings.LockedSlotsSystem_AutoSpreadRadius < 2f ? 2f : 50f;
                LogModWarning($"Setting '{nameof(Settings.LockedSlotsSystem_AutoSpreadRadius)}' value {v:0.0} is out of range (2..50). Value {Settings.LockedSlotsSystem_AutoSpreadRadius:0} will be used instead.");
            }
        }

        private static string LockedSlotsSavesDirectory => FeaturesFolder + $"\\{nameof(Settings.LockedSlotsSystem)}\\Saves";
        public static List<Vector3i> IgnoredContainers = new();

        /// <summary>
        /// Reads colors for slots.
        /// </summary>
        public class XUiC_ItemStack_ParseAttribute
        {
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
                return true;
            }
        }

        /// <summary>
        /// Custom getter for LockedSlots for vehicle storage.
        /// </summary>
        public class XUiC_ContainerStandardControls_LockedSlots_Getter
        {
            public struct APostfix
            {
                public XUiC_ContainerStandardControls __instance;
                public bool[] __result;
            }

            public static void Postfix(XUiC_ContainerStandardControls __instance, ref bool[] __result)
            {
                var isVehicle = __instance.GetParentByType<XUiC_VehicleStorageWindowGroup>() != null;
                if (isVehicle)
                {
                    var entityId = __instance.GetVehicleEntityId();
                    if (entityId != null && XUiC_ContainerStandardControls_Init.EntityLockedSlots.TryGetValue(entityId, out bool[] slots))
                    {
                        __result = slots;
                        return;
                    }
                    __result = Array.Empty<bool>();
                }
            }
        }

        /// <summary>
        /// Custom setter for LockedSlots for vehicle storage.
        /// </summary>
        public class XUiC_ContainerStandardControls_LockedSlots_Setter
        {
            public struct APrefix
            {
                public XUiC_ContainerStandardControls __instance;
                public bool[] value;
            }

            public static bool Prefix(XUiC_ContainerStandardControls __instance, bool[] value)
            {
                var isVehicle = __instance.GetParentByType<XUiC_VehicleStorageWindowGroup>() != null;
                if (isVehicle)
                {
                    var entityId = __instance.GetVehicleEntityId();
                    if (entityId != null)
                    {
                        XUiC_ContainerStandardControls_Init.EntityLockedSlots[entityId] = value ?? Array.Empty<bool>();
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Update locked slots on UI for opened vehicle storage.
        /// </summary>
        public class XUiC_ItemStackGrid_OnOpen
        {
            public static void Prefix(XUiC_ItemStackGrid __instance)
            {
                var vehicleWindows = __instance.GetParentByType<XUiC_VehicleStorageWindowGroup>();
                if (vehicleWindows != null)
                {
                    var controls = vehicleWindows.containerWindow.controls;
                    var entityId = controls.GetVehicleEntityId();
                    if (entityId != null)
                    {
                        LoadLockedSlots(entityId);
                        var lockedSlots = XUiC_ContainerStandardControls_Init.EntityLockedSlots[entityId];
                        controls.ApplyLockedSlotStates?.Invoke(lockedSlots);
                    }
                }
            }

            static void LoadLockedSlots(string entityId)
            {
                if (!string.IsNullOrEmpty(entityId))
                {
                    var entityLockedSlots = XUiC_ContainerStandardControls_Init.EntityLockedSlots;
                    if (!entityLockedSlots.ContainsKey(entityId))
                    {
                        var worldSeed = Helper.WorldSeed;
                        if (!string.IsNullOrEmpty(worldSeed))
                        {
                            var configFile = Path.Combine(LockedSlotsSavesDirectory, $"{worldSeed}-{entityId}.txt");
                            if (File.Exists(configFile))
                            {
                                entityLockedSlots[entityId] = JsonConvert.DeserializeObject<bool[]>(File.ReadAllText(configFile));
                                return;
                            }
                            entityLockedSlots[entityId] = Array.Empty<bool>();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update locked slots UI for all containers.
        /// </summary>
        public class XUiController_RefreshBindings
        {
            public static void Postfix(XUiController __instance)
            {
                if (__instance is XUiC_ItemStack itemStack)
                {
                    itemStack.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Prevent adding new items to empty locked slots in the backpack.
        /// </summary>
        public class Bag_AddItem
        {
            public struct AParams
            {
                public Bag __instance;
                public List<int> __state;
            }

            public static void Prefix(Bag __instance, ref List<int> __state)
            {
                var items = __instance.items;
                var lockedSlots = __instance.LockedSlots;
                if (items != null && lockedSlots != null && lockedSlots.Length >= items.Length)
                {
                    __state = new();
                    for (int i = 0; i < items.Length; i++)
                    {
                        // temporary make locked slots not empty
                        if (lockedSlots[i] && items[i].IsEmpty())
                        {
                            items[i].count = 1;
                            items[i].itemValue.type = 1;
                            __state.Add(i);
                        }
                    }
                }
            }

            public static void Postfix(Bag __instance, List<int> __state)
            {
                if (__state?.Count > 0)
                {
                    // restore empty locked slots
                    foreach (var i in __state)
                    {
                        __instance.items[i].count = 0;
                        __instance.items[i].itemValue.type = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Auto-spreads the loot to nearby containers.
        /// </summary>
        public class XUiC_ContainerStandardControls_Init
        {
            public static Dictionary<string, bool[]> EntityLockedSlots = new();
            public static bool Active = false;
            public static Vector3i? CurrentContainerPos = null;
            public static Vector3i? LocalOpenEntityPos = null;
            
            public static void Postfix(XUiC_ContainerStandardControls __instance)
            {
                // assign methods for locked slots for vehicles
                var isVehicle = __instance.GetParentByType<XUiC_VehicleStorageWindowGroup>() != null;
                if (isVehicle)
                {
                    __instance.ApplyLockedSlotStates ??= (bool[] _lockedSlots) => ApplyLockedSlotStates(__instance, _lockedSlots);
                    __instance.UpdateLockedSlotStates ??= (XUiC_ContainerStandardControls _csc) => UpdateLockedSlots(_csc);
                    __instance.LockModeToggled ??= () => LockModeToggled(__instance);

                    // method based on XUiC_BackpackWindow.ApplyLockedSlotStates
                    static void ApplyLockedSlotStates(XUiC_ContainerStandardControls controls, bool[] _lockedSlots)
                    {
                        var itemStackControllers = controls.GetItemStackGrid().GetItemStackControllers();
                        for (int i = 0; i < itemStackControllers.Length; i++)
                        {
                            itemStackControllers[i].UserLockedSlot = _lockedSlots != null && i < _lockedSlots.Length && _lockedSlots[i];
                        }
                    }

                    // method based on XUiC_BackpackWindow.UpdateLockedSlots
                    static void UpdateLockedSlots(XUiC_ContainerStandardControls _csc)
                    {
                        if (_csc != null)
                        {
                            var grid = _csc.GetItemStackGrid(); // my line of code
                            int slotCount = grid.itemControllers.Length;
                            bool[] array = _csc.LockedSlots ?? new bool[slotCount];
                            if (array.Length < slotCount)
                            {
                                bool[] array2 = new bool[slotCount];
                                Array.Copy(array, array2, array.Length);
                                array = array2;
                            }
                            XUiC_ItemStack[] itemStackControllers = grid.GetItemStackControllers();
                            for (int i = 0; i < itemStackControllers.Length && i < array.Length; i++)
                            {
                                array[i] = itemStackControllers[i].UserLockedSlot;
                            }
                            _csc.LockedSlots = array;
                        }
                    }

                    // method based on XUiC_BackpackWindow.LockModeToggled
                    static void LockModeToggled(XUiC_ContainerStandardControls controls)
                    {
                        controls.LockModeEnabled = !controls.LockModeEnabled;
                        var userLockMode = controls.LockModeEnabled;

                        { // additional code based on property setter XUiC_BackpackWindow.UserLockMode
                            if (userLockMode)
                            {
                                UpdateLockedSlots(controls);
                            }
                            controls?.LockModeChanged(userLockMode);
                            //userLockMode = value;
                            controls.WindowGroup.isEscClosable = !userLockMode;
                            controls.xui.playerUI.windowManager.GetModalWindow().isEscClosable = !userLockMode;
                            controls.RefreshBindings();
                        }

                        // save locked slots
                        if (!userLockMode)
                        {
                            UpdateLockedSlots(controls);
                            SaveLockedSlots(controls);
                        }
                    }

                    static void SaveLockedSlots(XUiC_ContainerStandardControls controls)
                    {
                        if (controls.xui.vehicle != null)
                        {
                            var entityId = controls.GetVehicleEntityId();
                            if (EntityLockedSlots.ContainsKey(entityId))
                            {
                                var worldSeed = Helper.WorldSeed;
                                if (!string.IsNullOrEmpty(worldSeed))
                                {
                                    var lockedSlots = controls.LockedSlots ?? Array.Empty<bool>();
                                    Directory.CreateDirectory(LockedSlotsSavesDirectory);
                                    File.WriteAllText(Path.Combine(LockedSlotsSavesDirectory, $"{worldSeed}-{entityId}.txt"), lockedSlots.Serialize());
                                    LogModInfo($"Locked slots for {entityId} saved.");
                                }
                            }
                        }
                    }
                }

                // auto-spread button
                var btnSpreadLoot = __instance.GetChildById("btnSpreadLoot");
                if (btnSpreadLoot != null)
                {
                    btnSpreadLoot.OnPress += (sender, _) => GameManager.Instance.StartCoroutine(SpreadLoot(sender));
                }

                IEnumerator SpreadLoot(XUiController sender)
                {
                    if (!Active)
                    {
                        Active = true;
                        var parentWindowController = sender.GetParentWindow().Controller;
                        var controls = sender.GetParentByType<XUiC_ContainerStandardControls>();
                        var localOpenContainer = controls.xui.lootContainer;
                        var isWorkstation = localOpenContainer == null && controls.xui.currentWorkstation?.Length > 0;
                        var workstation = isWorkstation ? GetWorkstation(controls.xui.currentWorkstation) : null;
                        LocalOpenEntityPos = isWorkstation ? workstation?.ToWorldPos() : localOpenContainer?.ToWorldPos();
                        var localOpenEntityId = isWorkstation ? workstation?.entityId : localOpenContainer?.EntityId;
                        
                        var loot = controls.GetItemStackGrid();
                        var lockedSlots = controls.xui.playerUI.entityPlayer.bag.LockedSlots;
                        var moveStartBottomRight = controls.MoveStartBottomRight;

                        if (LocalOpenEntityPos.HasValue)
                        {
                            if (localOpenContainer != null && !IgnoredContainers.Contains(LocalOpenEntityPos.Value))
                            {
                                controls.MoveSmart();
                            }
                            if (!IsServer)
                            {
                                // unlock container while spreading to be able to lock other containers
                                GameManager.Instance.TEUnlockServer(0, LocalOpenEntityPos.Value, localOpenEntityId.Value, _allowContainerDestroy: false);
                            }
                        }
                        else if (controls.xui.vehicle != null)
                        {
                            controls.MoveSmart();
                        }

                        var player = Helper.PlayerLocal;
                        var tiles = Helper.GetTileEntities(player.position, Settings.LockedSlotsSystem_AutoSpreadRadius);
                        var timeLimit = Time.time + 5f;
                        
                        foreach (var tile in tiles)
                        {
                            var tilePos = tile.ToWorldPos();
                            ITileEntityLootable box = null;

                            if (tile is TileEntityComposite teComposite && teComposite.TryGetSelfOrFeature<ITileEntityLootable>(out var teLootable))
                            {
                                box = teLootable;
                            }

                            box ??= tile as ITileEntityLootable;

                            if (box != null && box.bTouched && !IgnoredContainers.Contains(tilePos) && tilePos != LocalOpenEntityPos)
                            {
                                var secureBox = box as TileEntitySecureLootContainer;
                                var hasAccess = secureBox == null || !secureBox.IsLocked() || secureBox.IsUserAllowed(PlatformManager.InternalLocalUserIdentifier);
                                if (hasAccess)
                                {
                                    var tileEntityId = tile.entityId;
                                    var boxPos = tilePos;
                                    var deniedPos = boxPos.Invert();
                                    var openerEntityId = GameManager.Instance.GetEntityIDForLockedTileEntity(tile);
                                    var isOpenByAnyPlayer = openerEntityId > 0; // this variable can be used on server only
                                    if (IsServer)
                                    {
                                        if (!isOpenByAnyPlayer)
                                        {
                                            Spread();
                                        }
                                        else if (LocalPlayerUI.GetUIForPlayer(player) != null && box.ContainsAnyItem(loot))
                                        {
                                            GameManager.ShowTooltip(player, Localization.Get("ttNoInteractItem"), string.Empty, "ui_denied");
                                        }
                                    }
                                    else if (box.ContainsAnyItem(loot))
                                    {
                                        // send "TELockServer" and wait until response processed in "TEAccessClient" and then call Spread()
                                        CurrentContainerPos = boxPos;
                                        GameManager.Instance.TELockServer(0, boxPos, tileEntityId, player.entityId, null);

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
                                        XUiM_LootContainer.StashItems(parentWindowController, loot, box, 0, lockedSlots, XUiM_LootContainer.EItemMoveKind.FillAndCreate, moveStartBottomRight);
                                    }

                                    void LeaveBox()
                                    {
                                        GameManager.Instance.TEUnlockServer(0, boxPos, tileEntityId, _allowContainerDestroy: false);
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
                var btnSpreadReceiver = __instance.GetChildById("btnSpreadReceiver");
                var btnSpreadIgnorer = __instance.GetChildById("btnSpreadIgnorer");
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
                    ThreadManager.StartCoroutine(lootWindow.closeInventoryLater());
                }
            }

            public static void SaveIgnoredContainers()
            {
                var playerId = Helper.PlayerId;
                var worldSeed = Helper.WorldSeed;
                Directory.CreateDirectory(LockedSlotsSavesDirectory);
                var filePath = Path.Combine(LockedSlotsSavesDirectory, $"{worldSeed}-ignored-containers-p{playerId}.txt");
                File.WriteAllLines(filePath, IgnoredContainers.Select(pos => pos.ToString()));
            }

            public static void LoadIgnoredContainers()
            {
                IgnoredContainers.Clear();
                var playerId = Helper.PlayerId;
                var worldSeed = Helper.WorldSeed;
                var filePath = Path.Combine(LockedSlotsSavesDirectory, $"{worldSeed}-ignored-containers-p{playerId}.txt");
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
        /// Custom binding values for item stacks.
        /// </summary>
        public class XUiC_ItemStack_GetBindingValue
        {
            public struct APrefix
            {
                public XUiC_ItemStack __instance;
                public string _value;
                public string _bindingName;
                public bool __result;
            }

            public struct APostfix
            {
                public XUiC_ItemStack __instance;
                public string _value;
                public string _bindingName;
            }

            public static bool Prefix(XUiC_ItemStack __instance, ref string _value, string _bindingName, ref bool __result)
            {
                if (_bindingName == "userlockmode")
                {
                    var parent = __instance.GetParentByType<XUiC_VehicleContainer>();
                    if (parent != null)
                    {
                        _value = (parent.controls?.LockModeEnabled ?? false).ToString();
                        __result = true;
                        return false;
                    }
                }
                return true;
            }

            public static void Postfix(XUiC_ItemStack __instance, ref string _value, string _bindingName)
            {
                if (__instance.UserLockedSlot)
                {
                    if (_bindingName == "backgroundcolor")
                    {
                        _value = __instance.backgroundcolorFormatter.Format(__instance.AttributeLock
                            ? Color32.Lerp(__instance.attributeLockColor, XUiC_ItemStack_ParseAttribute.LockColor, 0.5f)
                            : XUiC_ItemStack_ParseAttribute.LockColor);
                    }
                    if (_bindingName == "selectionbordercolor")
                    {
                        _value = __instance.selectionbordercolorFormatter.Format(
                            Color32.Lerp(__instance.SelectionBorderColor, XUiC_ItemStack_ParseAttribute.LockColor, 0.5f));
                    }
                }
            }
        }

        /// <summary>
        /// Turn off lock mode on vehicle container close.
        /// </summary>
        public class XUiC_ItemStackGrid_OnClose
        {
            public static void Prefix(XUiC_ItemStackGrid __instance)
            {
                var vehicleWindows = __instance.GetParentByType<XUiC_VehicleStorageWindowGroup>();
                if (vehicleWindows != null)
                {
                    var controls = vehicleWindows.containerWindow.controls;
                    if (controls.LockModeEnabled)
                    {
                        controls.LockModeToggled?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Custom binding values for loot containers.
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
                /*if (_bindingName == "is_drone")
                {
                    _value = (__instance.xui.lootContainer != null && __instance.xui.lootContainer.GetChunk() == null && __instance.xui.lootContainer.lootListName == "roboticDrone").ToString();
                    __result = true;
                    return false;
                }*/
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
