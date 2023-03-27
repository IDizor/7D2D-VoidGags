using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Platform;
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
                Debug.Log($"Mod {nameof(VoidGags)}: Patch '{nameof(Settings.LockedSlotsSystem)}' is not applicable for Undead Legacy.");
                return;
            }

            UseXmlPatches(nameof(Settings.LockedSlotsSystem));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStack), "ParseAttribute"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStack_ParseAttribute_Params p) =>
                XUiC_ItemStack_ParseAttribute.Prefix(ref p.__result, p._name, p._value))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ItemStackGrid), "OnOpen"),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ItemStackGrid __instance) => XUiC_ItemStackGrid_OnOpen.Prefix(__instance))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ContainerStandardControls), "ChangeLockedSlots"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ContainerStandardControls_ChangeLockedSlots_Params p) =>
                XUiC_ContainerStandardControls_ChangeLockedSlots.Postfix(p.__instance, p._newValue))));

            harmony.Patch(AccessTools.Method(typeof(XUiC_ContainerStandardControls), "Init"), null,
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_ContainerStandardControls __instance) => XUiC_ContainerStandardControls_Init.Postfix(__instance))));

            Debug.Log($"Mod {nameof(VoidGags)}: Patch applied - {nameof(Settings.LockedSlotsSystem)}");
        }

        private struct XUiC_ContainerStandardControls_ChangeLockedSlots_Params
        {
            public XUiC_ContainerStandardControls __instance;
            public long _newValue;
        }

        private struct XUiC_ItemStack_ParseAttribute_Params
        {
            public bool __result;
            public string _name;
            public string _value;
        }

        /// <summary>
        /// Reads colors for slots.
        /// </summary>
        public class XUiC_ItemStack_ParseAttribute
        {
            public static Color32 RegularColor;
            public static Color32 LockColor = new Color32(100, 20, 20, 255);

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
            private static string LockedSlotsConfigDirectory => ModFolder + $"\\{nameof(Settings.LockedSlotsSystem)}\\Saves";

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
                        Directory.CreateDirectory(LockedSlotsConfigDirectory);
                        File.WriteAllText(Path.Combine(LockedSlotsConfigDirectory, $"{entityId}-{worldSeed}.txt"), lockedSlots.ToString());
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
                        var configFile = Path.Combine(LockedSlotsConfigDirectory, $"{entityId}-{worldSeed}.txt");
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
            public static void Postfix(XUiC_ContainerStandardControls __instance, long _newValue)
            {
                var isVehicleGroup = __instance.xui.vehicle != null && __instance.GetParentByType<XUiC_VehicleStorageWindowGroup>() != null;
                var isPlayerBackpack = __instance.GetParentByType<XUiC_BackpackWindow>() != null;
                var entityId = isPlayerBackpack
                    ? "p" + Helper.PlayerId
                    : isVehicleGroup
                        ? "v" + __instance.xui.vehicle.entityId.ToString()
                        : null;

                if (entityId != null)
                {
                    XUiC_ItemStackGrid_OnOpen.SaveLockedSlotsCount((int)_newValue, entityId);
                    var stackGrid = __instance.GetItemStackGrid();
                    if (stackGrid != null)
                    {
                        var itemControllers = stackGrid.GetItemStackControllers();
                        var backgroundColor = AccessTools.Field(typeof(XUiC_ItemStack), "backgroundColor");
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
        }

        /// <summary>
        /// Auto-spreads the loot to nearby containers.
        /// </summary>
        public class XUiC_ContainerStandardControls_Init
        {
            public static void Postfix(XUiC_ContainerStandardControls __instance)
            {
                XUiController btnSpreadLoot = __instance.GetChildById("btnSpreadLoot");
                if (btnSpreadLoot != null)
                {
                    btnSpreadLoot.OnPress += delegate
                    {
                        var player = GameManager.Instance.World.GetPrimaryPlayer();
                        var tiles = Helper.GetTileEntities(player.position, 10f);
                        var stashLockedSlots = AccessTools.Field(typeof(XUiC_ContainerStandardControls), "stashLockedSlots");
                        foreach (var tile in tiles)
                        {
                            if (tile is TileEntityLootContainer box)
                            {
                                var secure = box as TileEntitySecureLootContainer;
                                var hasAccess = secure == null || secure.IsUserAllowed(PlatformManager.InternalLocalUserIdentifier);
                                if (box.bTouched && hasAccess)
                                {
                                    var stackGrid = __instance.GetItemStackGrid();
                                    if (stackGrid != null)
                                    {
                                        var lockedSlots = (int)stashLockedSlots.GetValue(__instance);
                                        XUiM_LootContainer.StashItems(stackGrid, box, lockedSlots, XUiM_LootContainer.EItemMoveKind.FillAndCreate, __instance.MoveStartBottomRight);
                                    }
                                }
                            }
                        }
                    };
                }
            }
        }
    }
}
