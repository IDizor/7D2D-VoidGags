using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Platform;
using UniLinq;
using UnityEngine;
using static Entity;
using static VoidGags.VoidGags.AutoSpreadLoot;

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
                LogWarning($"Patch '{nameof(Settings.AutoSpreadLoot)}' is not compatible with Undead Legacy.");
                return;
            }

            UseXmlPatches(nameof(Settings.AutoSpreadLoot));
            
            Harmony.Patch(AccessTools.Method(typeof(Bag), nameof(Bag.AddItem)),
                prefix: new HarmonyMethod(SomeStorage_AddItem.Prefix),
                postfix: new HarmonyMethod(SomeStorage_AddItem.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(TEFeatureStorage), nameof(TEFeatureStorage.AddItem)),
                prefix: new HarmonyMethod(SomeStorage_AddItem.Prefix),
                postfix: new HarmonyMethod(SomeStorage_AddItem.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(ItemStack), nameof(ItemStack.IsEmpty), parameters: []),
                postfix: new HarmonyMethod(ItemStack_IsEmpty.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_ContainerStandardControls), nameof(XUiC_ContainerStandardControls.Init)),
                postfix: new HarmonyMethod(XUiC_ContainerStandardControls_Init.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(WorldBase), nameof(WorldBase.SetBlockRPC), [typeof(BlockValueRef), typeof(BlockValue)]),
                prefix: new HarmonyMethod(WorldBase_SetBlockRPC.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(XUiController), nameof(XUiController.GetBindingValue)),
                prefix: new HarmonyMethod(XUiController_GetBindingValue.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(LockManager), nameof(LockManager.LockResponse)),
                postfix: new HarmonyMethod(LockManager_LockResponse.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(ItemActionEntryEquip), nameof(ItemActionEntryEquip.OnActivated)),
                prefix: new HarmonyMethod(ItemActionEntryEquip_OnActivated.Prefix),
                postfix: new HarmonyMethod(ItemActionEntryEquip_OnActivated.Postfix));

            Harmony.Patch(AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.AddItem), [typeof(ItemStack)]),
                prefix: new HarmonyMethod(XUiM_PlayerInventory_AddItem.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(GUIWindowManager), nameof(GUIWindowManager.CloseAllOpenModalWindows), [typeof(GUIWindow), typeof(bool)]),
                prefix: new HarmonyMethod(GUIWindowManager_CloseAllOpenModalWindows.Prefix));

            Harmony.Patch(AccessTools.Method(typeof(TEFeatureStorage), nameof(TEFeatureStorage.ShowUI)),
                prefix: new HarmonyMethod(TEFeatureStorage_ShowUI.Prefix));

            OnGameLoadedActions.Add(XUiC_ContainerStandardControls_Init.LoadIgnoredContainers);

            if (Settings.AutoSpreadLoot_Radius < 2f || Settings.AutoSpreadLoot_Radius > 50f)
            {
                var v = Settings.AutoSpreadLoot_Radius;
                Settings.AutoSpreadLoot_Radius = Settings.AutoSpreadLoot_Radius < 2f ? 2f : 50f;
                LogWarning($"Setting '{nameof(Settings.AutoSpreadLoot_Radius)}' value {v:0.0} is out of range (2..50). Value {Settings.AutoSpreadLoot_Radius:0} will be used instead.");
            }
        }

        public static class AutoSpreadLoot
        {
            public static string SavesDir => FeaturesFolderPath + $"\\{nameof(Settings.AutoSpreadLoot)}\\Saves";

            public static List<Vector3i> IgnoredContainers = [];
            public static List<XUiC_ItemStackGrid> UiGrids = null;
            public static bool SpreadingActive = false;
            public static Vector3i? IterationTilePos = null;
            public static Vector3i? LocalOpenEntityPos = null;

            /// <summary>
            /// Keep all UI grids to prevent adding new items to empty locked slots in the backpack/vehicle/drone.
            /// </summary>
            public static class SomeStorage_AddItem
            {
                public static void Prefix()
                {
                    if (IsDedicatedServer) return;
                    UiGrids = Helper.PlayerLocal.PlayerUI?.activeItemStackGrids;
                    /*foreach (var g in UiGrids)
                    {
                        LogModWarning($"Grid: {g.GetType().Name}, {g.Parent.GetType().Name}, {g.Parent.Parent?.GetType().Name}, {g.Parent.Parent?.Parent?.GetType().Name}, {g.Parent.Parent?.Parent?.Parent?.GetType().Name}");
                    }*/
                }

                public static void Postfix()
                {
                    if (IsDedicatedServer) return;
                    UiGrids = null;
                }
            }

            /// <summary>
            /// Return false for locked slots when adding new items.
            /// </summary>
            public static class ItemStack_IsEmpty
            {
                public static void Postfix(ItemStack __instance, ref bool __result)
                {
                    if (IsDedicatedServer) return;

                    if (__result && UiGrids != null)
                    {
                        foreach (var grid in UiGrids)
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
                            if (grid is XUiC_BagContainer bagTypedContainer)
                            {
                                var bag = bagTypedContainer.xui.Vehicle?.CurrentVehicle?.bag;
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
                                if (loot.xui.LootContainer != null)
                                {
                                    var i = Array.FindIndex(loot.GetSlots(), item => item != null && ReferenceEquals(item, __instance));
                                    if (i >= 0 && loot.xui.LootContainer.SlotLocks.Get(i))
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
            public static class XUiC_ContainerStandardControls_Init
            {
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
                        if (!SpreadingActive)
                        {
                            SpreadingActive = true;
                            var parentWindowController = sender.GetParentWindow().Controller;
                            var controls = sender.GetParentByType<XUiC_ContainerStandardControls>();
                            var localOpenContainer = controls.xui.LootContainer;
                            var workstationGroup = localOpenContainer == null ? controls.GetParentByType<XUiC_WorkstationWindowGroup>() : null;
                            var isWorkstation = workstationGroup != null && workstationGroup.WorkstationData != null;
                            var workstation = isWorkstation ? workstationGroup.WorkstationData.TileEntity : null;
                            LocalOpenEntityPos = isWorkstation ? workstation?.ToWorldPos() : localOpenContainer?.ToWorldPos();

                            var loot = controls.GetItemStackGrid();
                            var lockedSlots = controls.xui.playerUI.entityPlayer.bag.LockedSlots;
                            var moveStartBottomRight = controls.MoveStartBottomRight;

                            //LogWarning($"Local active container = {LocalOpenEntityPos.HasValue}");
                            if (LocalOpenEntityPos.HasValue)
                            {
                                if (!IgnoredContainers.Contains(LocalOpenEntityPos.Value))
                                {
                                    controls.MoveSmart();
                                }

                                // unlock container while spreading to be able to lock other containers
                                LockManager.Instance.UnlockRequestLocal();
                            }
                            else if (controls.xui.Vehicle?.CurrentVehicle != null)
                            {
                                controls.MoveSmart();
                                // unlock vehicle while spreading to be able to lock other containers
                                LockManager.Instance.UnlockRequestLocal();
                            }

                            var player = Helper.PlayerLocal;
                            var tiles = Helper.GetTileEntities(player.position, Settings.AutoSpreadLoot_Radius);
                            var timeLimit = Time.time + 3f;
                            //LogWarning($"Tiles count = {tiles.Length}");
                            foreach (var tile in tiles)
                            {
                                var tilePos = tile.ToWorldPos();
                                if (tilePos == LocalOpenEntityPos)
                                {
                                    continue;
                                }

                                tile.TryGetSelfOrFeature(out ITileEntityLootable teLootable);

                                if (teLootable != null && teLootable.bTouched && !IgnoredContainers.Contains(tilePos))
                                {
                                    var featureStorage = teLootable as TEFeatureStorage;
                                    var isLocked = featureStorage?.lockFeature != null && featureStorage.lockFeature.IsLocked() && !featureStorage.lockFeature.IsUserAllowed(PlatformManager.InternalLocalUserIdentifier);
                                    //LogWarning($"Have Access = {!isLocked}");
                                    if (!isLocked)
                                    {
                                        if (teLootable.ContainsAnyItem(loot))
                                        {
                                            //LogWarning($"Contains similar items.");
                                            IterationTilePos = tilePos;

                                            /// Send lock request and wait until response
                                            /// processed in <see cref="LockManager_LockResponse"/>
                                            /// and then call StashItems()
                                            LockManager.Instance.LockRequestLocal(teLootable);

                                            var timedOut = false;
                                            var waitFlag = true;
                                            var deniedPos = tilePos.Invert();
                                            while (waitFlag)
                                            {
                                                yield return new WaitForSeconds(0.02f);
                                                var access = IterationTilePos == null;
                                                var denied = IterationTilePos == deniedPos;
                                                timedOut = Time.time > timeLimit && !access && !denied;
                                                waitFlag = !access && !denied && !timedOut;
                                                if (access)
                                                {
                                                    StashItems();
                                                    LockManager.Instance.UnlockRequestLocal();
                                                }
                                            }

                                            if (timedOut)
                                            {
                                                Reset();
                                                LockManager.Instance.UnlockRequestLocal();
                                                LocalPlayerUI.GetUIForPrimaryPlayer().windowManager.CloseAllOpenModalWindows();
                                                LocalOpenEntityPos = null;
                                            }
                                        }

                                        void StashItems()
                                        {
                                            XUiM_LootContainer.StashItems(parentWindowController, loot, teLootable, 0, lockedSlots,
                                                XUiM_LootContainer.EItemMoveKind.FillAndCreate, moveStartBottomRight);
                                        }
                                    }
                                }
                            }

                            Reset();

                            // Lock the container again after spreading loot to other containers (if it was locked before)
                            if (LocalOpenEntityPos.HasValue)
                            {
                                LockManager.Instance.LockRequestLocal(isWorkstation ? workstation : localOpenContainer);
                            }
                            else if (controls.xui.Vehicle?.CurrentVehicle != null)
                            {
                                var vehicle = controls.xui.Vehicle.CurrentVehicle;
                                LockManager.Instance.LockRequestLocal(vehicle, new EntityLockContext("storage", vehicle.bag));
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
                        var lootContainer = sender.xui.LootContainer;
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

                public static void SaveIgnoredContainers()
                {
                    var playerId = Helper.PlayerId;
                    var worldSeed = Helper.WorldSeed;
                    Directory.CreateDirectory(SavesDir);
                    var filePath = Path.Combine(SavesDir, $"{worldSeed}-ignored-containers-p{playerId}.txt");
                    File.WriteAllLines(filePath, IgnoredContainers.Select(pos => pos.ToString()));
                }

                public static void LoadIgnoredContainers()
                {
                    IgnoredContainers.Clear();
                    var playerId = Helper.PlayerId;
                    var worldSeed = Helper.WorldSeed;
                    var filePath = Path.Combine(SavesDir, $"{worldSeed}-ignored-containers-p{playerId}.txt");
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

                public static void Reset()
                {
                    IterationTilePos = null;
                    SpreadingActive = false;
                }
            }

            /// <summary>
            /// Removes block from the ignored containers when destroyed.
            /// </summary>
            public static class WorldBase_SetBlockRPC
            {
                public static void Prefix(BlockValueRef _bvRef, BlockValue _blockValue)
                {
                    if (IsDedicatedServer) return;

                    if (_blockValue.isair || GameManager.Instance.World.GetBlock(_bvRef.BlockPosition).isair)
                    {
                        if (IgnoredContainers.Remove(_bvRef.BlockPosition))
                        {
                            XUiC_ContainerStandardControls_Init.SaveIgnoredContainers();
                        }
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
                        if (_bindingName == "auto_spread_ignore")
                        {
                            _value = "false";
                            var containerPos = __instance.xui.LootContainer?.ToWorldPos();
                            if (containerPos.HasValue)
                            {
                                _value = IgnoredContainers.Contains(containerPos.Value).ToString();
                            }
                            __result = true;
                            return false;
                        }
                        if (_bindingName == "is_container_block")
                        {
                            _value = (__instance.xui.LootContainer != null && __instance.xui.LootContainer.GetChunk() != null).ToString();
                            __result = true;
                            return false;
                        }
                    }
                    return true;
                }
            }

            /// <summary>
            /// Keep "Equip" item action state.
            /// </summary>
            public static class ItemActionEntryEquip_OnActivated
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
            public static class XUiM_PlayerInventory_AddItem
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
            /// Check if current container (from spreading loop) is locked/busy.
            /// Inverts its position to mark it as locked.
            /// </summary>
            public static class LockManager_LockResponse
            {
                public static void Postfix(bool _success, string _errorMsg, ReadOnlySpan<ILockTarget> _targets)
                {
                    if (SpreadingActive && IterationTilePos.HasValue)
                    {
                        //LogWarning($"Lock Response succeeded = {_success}");
                        if (_targets != null && _targets.Length == 1)
                        {
                            var target = _targets[0];
                            var blockPos = Vector3i.zero;

                            if (target is TileEntity tileEntity)
                            {
                                blockPos = tileEntity.ToWorldPos();
                            }
                            else if (target is TEFeatureAbs tEFeatureAbs)
                            {
                                blockPos = tEFeatureAbs.ToWorldPos();
                            }

                            //LogWarning($"Container pos is: {blockPos}");
                            if (blockPos != Vector3i.zero)
                            {
                                if (IterationTilePos == blockPos)
                                {
                                    IterationTilePos = _success ? null : blockPos.Invert();
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Suppress <see cref="GUIWindowManager.CloseAllOpenModalWindows"/> when spreading is active.
            /// </summary>
            public static class GUIWindowManager_CloseAllOpenModalWindows
            {
                public static bool Prefix()
                {
                    return !SpreadingActive;
                }
            }

            /// <summary>
            /// Suppress <see cref="TEFeatureStorage.ShowUI"/> when spreading is active.
            /// </summary>
            public static class TEFeatureStorage_ShowUI
            {
                public static bool Prefix(bool _lockGranted)
                {
                    if (_lockGranted)
                    {
                        return !SpreadingActive;
                    }
                    return true;
                }
            }
        }
    }
}
