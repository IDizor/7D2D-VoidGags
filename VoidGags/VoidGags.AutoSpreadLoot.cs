using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        public void ApplyPatches_AutoSpreadLoot()
        {
            LogApplyingPatch(nameof(Settings.AutoSpreadLoot));

            if (IsUndeadLegacy)
            {
                LogModWarning($"Patch '{nameof(Settings.AutoSpreadLoot)}' is not compatible with Undead Legacy.");
                return;
            }

            UseXmlPatches(nameof(Settings.AutoSpreadLoot));
            
            Harmony.Patch(AccessTools.Method(typeof(Bag), nameof(Bag.AddItem)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => SomeStorage_AddItem.Prefix())),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => SomeStorage_AddItem.Postfix())));

            Harmony.Patch(AccessTools.Method(typeof(TileEntityLootContainer), nameof(TileEntityLootContainer.AddItem)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => SomeStorage_AddItem.Prefix())),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => SomeStorage_AddItem.Postfix())));

            Harmony.Patch(AccessTools.Method(typeof(TEFeatureStorage), nameof(TEFeatureStorage.AddItem)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => SomeStorage_AddItem.Prefix())),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => SomeStorage_AddItem.Postfix())));

            Harmony.Patch(AccessTools.Method(typeof(ItemStack), nameof(ItemStack.IsEmpty), parameters: []),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemStack_IsEmpty.APostfix p) => ItemStack_IsEmpty.Postfix(p.__instance, ref p.__result))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ContainerStandardControls), nameof(XUiC_ContainerStandardControls.Init)),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ContainerStandardControls __instance) => XUiC_ContainerStandardControls_Init.Postfix(__instance))));

            Harmony.Patch(AccessTools.Method(typeof(World), nameof(World.SetBlockRPC), [typeof(int), typeof(Vector3i), typeof(BlockValue)]),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((World_SetBlockRPC.APrefix p) => World_SetBlockRPC.Prefix(p._blockPos, p._blockValue))));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_LootWindow), nameof(XUiC_LootWindow.GetBindingValue)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_LootWindow_GetBindingValue.APrefix p) => XUiC_LootWindow_GetBindingValue.Prefix(p.__instance, ref p._value, p._bindingName, ref p.__result))));

            Harmony.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.TEAccessClient)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vector3i _blockPos) => GameManager_TEAccessClient.Prefix(_blockPos))));

            Harmony.Patch(AccessTools.Method(typeof(ItemActionEntryEquip), nameof(ItemActionEntryEquip.OnActivated)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => ItemActionEntryEquip_OnActivated.Prefix())),
                postfix: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => ItemActionEntryEquip_OnActivated.Postfix())));

            Harmony.Patch(AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.AddItem), [typeof(ItemStack)]),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((bool __result) => XUiM_PlayerInventory_AddItem.Prefix(ref __result))));

            Harmony.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.TEDeniedAccessClient)),
                prefix: new HarmonyMethod(SymbolExtensions.GetMethodInfo((Vector3i _blockPos) => GameManager_TEDeniedAccessClient.Prefix(_blockPos))));

            OnGameLoadedActions.Add(XUiC_ContainerStandardControls_Init.LoadIgnoredContainers);

            if (Settings.AutoSpreadLoot_Radius < 2f || Settings.AutoSpreadLoot_Radius > 50f)
            {
                var v = Settings.AutoSpreadLoot_Radius;
                Settings.AutoSpreadLoot_Radius = Settings.AutoSpreadLoot_Radius < 2f ? 2f : 50f;
                LogModWarning($"Setting '{nameof(Settings.AutoSpreadLoot_Radius)}' value {v:0.0} is out of range (2..50). Value {Settings.AutoSpreadLoot_Radius:0} will be used instead.");
            }
        }

        public static class AutoSpreadLoot
        {
            public static List<Vector3i> IgnoredContainers = [];
            public static List<XUiC_ItemStackGrid> UiGrids = null;

            public static string SavesDir => FeaturesFolderPath + $"\\{nameof(Settings.AutoSpreadLoot)}\\Saves";
        }

        /// <summary>
        /// Keep all UI grids to prevent adding new items to empty locked slots in the backpack/vehicle/drone.
        /// </summary>
        public class SomeStorage_AddItem
        {
            public static void Prefix()
            {
                if (IsDedicatedServer) return;
                AutoSpreadLoot.UiGrids = Helper.PlayerLocal.PlayerUI?.activeItemStackGrids;
                /*foreach (var g in UiGrids)
                {
                    LogModWarning($"Grid: {g.GetType().Name}, {g.Parent.GetType().Name}, {g.Parent.Parent?.GetType().Name}, {g.Parent.Parent?.Parent?.GetType().Name}, {g.Parent.Parent?.Parent?.Parent?.GetType().Name}");
                }*/
            }

            public static void Postfix()
            {
                if (IsDedicatedServer) return;
                AutoSpreadLoot.UiGrids = null;
            }
        }

        /// <summary>
        /// Return false for locked slots when adding new items.
        /// </summary>
        public class ItemStack_IsEmpty
        {
            public struct APostfix
            {
                public ItemStack __instance;
                public bool __result;
            }

            public static void Postfix(ItemStack __instance, ref bool __result)
            {
                if (IsDedicatedServer) return;

                if (__result && AutoSpreadLoot.UiGrids != null)
                {
                    foreach (var grid in AutoSpreadLoot.UiGrids)
                    {
                        if (grid is XUiC_Backpack backpack)
                        {
                            var bag = backpack.xui.PlayerInventory?.backpack;
                            if (bag != null)
                            {
                                var i = Array.FindIndex(bag.items, item => item != null && ReferenceEquals(item, __instance));
                                if (i >= 0 && bag.LockedSlots.Get(i))
                                {
                                    __result = false;
                                    return;
                                }
                            }
                        }
                        if (grid is XUiC_VehicleContainer vehicleContainer)
                        {
                            var bag = vehicleContainer.xui.vehicle?.bag;
                            if (bag != null)
                            {
                                var i = Array.FindIndex(bag.items, item => item != null && ReferenceEquals(item, __instance));
                                if (i >= 0 && bag.LockedSlots.Get(i))
                                {
                                    __result = false;
                                    return;
                                }
                            }
                        }
                        if (grid is XUiC_LootContainer loot)
                        {
                            if (loot.xui.lootContainer != null)
                            {
                                var i = Array.FindIndex(loot.GetSlots(), item => item != null && ReferenceEquals(item, __instance));
                                if (i >= 0 && loot.xui.lootContainer.SlotLocks.Get(i))
                                {
                                    __result = false;
                                    return;
                                }
                            }
                        }
                    }
                    // check if it tries to add an item to the player bag
                    var playerBag = Helper.PlayerLocal?.bag;
                    if (playerBag != null)
                    {
                        var i = Array.FindIndex(playerBag.items, item => item != null && ReferenceEquals(item, __instance));
                        if (i >= 0 && playerBag.LockedSlots.Get(i))
                        {
                            __result = false;
                            return;
                        }
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
            
            public static void Postfix(XUiC_ContainerStandardControls __instance)
            {
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
                            if (localOpenContainer != null && !AutoSpreadLoot.IgnoredContainers.Contains(LocalOpenEntityPos.Value))
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
                        var tiles = Helper.GetTileEntities(player.position, Settings.AutoSpreadLoot_Radius);
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

                            if (box != null && box.bTouched && !AutoSpreadLoot.IgnoredContainers.Contains(tilePos) && tilePos != LocalOpenEntityPos)
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
                        if (!AutoSpreadLoot.IgnoredContainers.Remove(pos))
                        {
                            AutoSpreadLoot.IgnoredContainers.Add(pos);
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
                Directory.CreateDirectory(AutoSpreadLoot.SavesDir);
                var filePath = Path.Combine(AutoSpreadLoot.SavesDir, $"{worldSeed}-ignored-containers-p{playerId}.txt");
                File.WriteAllLines(filePath, AutoSpreadLoot.IgnoredContainers.Select(pos => pos.ToString()));
            }

            public static void LoadIgnoredContainers()
            {
                AutoSpreadLoot.IgnoredContainers.Clear();
                var playerId = Helper.PlayerId;
                var worldSeed = Helper.WorldSeed;
                var filePath = Path.Combine(AutoSpreadLoot.SavesDir, $"{worldSeed}-ignored-containers-p{playerId}.txt");
                if (File.Exists(filePath))
                {
                    foreach (var line in File.ReadAllLines(filePath))
                    {
                        var pos = Vector3i.Parse(line);
                        if (pos != Vector3i.zero)
                        {
                            AutoSpreadLoot.IgnoredContainers.Add(pos);
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
                if (IsDedicatedServer) return;

                if (_blockValue.isair || GameManager.Instance.World.GetBlock(_blockPos).isair)
                {
                    if (AutoSpreadLoot.IgnoredContainers.Remove(_blockPos))
                    {
                        XUiC_ContainerStandardControls_Init.SaveIgnoredContainers();
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
                        _value = AutoSpreadLoot.IgnoredContainers.Contains(containerPos.Value).ToString();
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
                return true;
            }
        }

        /// <summary>
        /// Keep "Equip" item action state.
        /// </summary>
        public class ItemActionEntryEquip_OnActivated
        {
            public static bool Active = false;

            public static void Prefix()
            {
                Active = true;
            }

            public static void Postfix()
            {
                Active = false;
            }
        }

        /// <summary>
        /// Returns false that means item was not added to the empty/not-filled-up stack.
        /// It is expected that the item from the toolbelt will be placed to the same slot where
        /// the equipping item was in the backpack (<see cref="ItemActionEntryEquip.OnActivated"/>).
        /// </summary>
        public class XUiM_PlayerInventory_AddItem
        {
            public static bool Prefix(ref bool __result)
            {
                if (ItemActionEntryEquip_OnActivated.Active)
                {
                    __result = false;
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
