using HarmonyLib;
using UniLinq;
using UnityEngine;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_ArrowsBoltsAutoPickUp()
        {
            LogApplyingPatch(nameof(Settings.ArrowsBoltsAutoPickUp));

            Harmony.Patch(AccessTools.Method(typeof(XUiC_Toolbelt), nameof(XUiC_Toolbelt.Update)),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((XUiC_Toolbelt __instance) => XUiC_Toolbelt_Update_2.Prefix(__instance))));

            Harmony.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.ItemDropServer), [typeof(ItemStack), typeof(Vector3), typeof(Vector3), typeof(int), typeof(float), typeof(bool)]),
                new HarmonyMethod(SymbolExtensions.GetMethodInfo((ItemStack _itemStack) => GameManager_ItemDropServer.Prefix(_itemStack))));
        }

        /// <summary>
        /// Pick up arrows and bolts automatically.
        /// </summary>
        public class XUiC_Toolbelt_Update_2
        {
            public static float SkipTime = -10f;

            public static void Prefix(XUiC_Toolbelt __instance)
            {
                if (Time.time > __instance.updateTime)
                {
                    var world = GameManager.Instance.World;
                    var player = Helper.PlayerLocal;
                    
                    if (player != null && !player.IsDead() && player.AttachedToEntity == null)
                    {
                        // collect sticky projectiles
                        if (ProjectileManager.projectiles != null)
                        {
                            foreach (var transform in ProjectileManager.projectiles.valueList)
                            {
                                if (transform != null && transform.TryGetComponent(out ProjectileMoveScript script))
                                {
                                    if (script != null && script.itemProjectile != null && script.itemProjectile.IsSticky)
                                    {
                                        var distance = (player.position - transform.position - Origin.position).magnitude;
                                        if (distance < Constants.cCollectItemDistance)
                                        {
                                            var dude = transform.GetComponentInParent<EntityAlive>();
                                            if (dude == null || dude.IsDead()) // do not auto-collect from alive entities
                                            {
                                                var itemStack = new ItemStack(script.itemValueProjectile, 1);
                                                if (player.inventory.CanTakeItem(itemStack) || player.bag.CanTakeItem(itemStack))
                                                {
                                                    player.PlayerUI.xui.PlayerInventory.AddItem(itemStack);
                                                    script.ProjectileID = -1;
                                                    UnityEngine.Object.Destroy(script.gameObject);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // collect dropped arrows
                        if (Time.time > SkipTime)
                        {
                            foreach (var entity in world.Entities.dict.Values.ToArray())
                            {
                                if (entity is EntityItem item && item.itemStack?.count == 1 && item.CanCollect())
                                {
                                    var distance = (player.position - item.position).magnitude;
                                    if (distance < Constants.cCollectItemDistance)
                                    {
                                        if (item.itemClass.IsSticky)
                                        {
                                            if ((player.inventory.CanTakeItem(item.itemStack) || player.bag.CanTakeItem(item.itemStack)))
                                            {
                                                GameManager.Instance.CollectEntityServer(item.entityId, player.entityId);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Time delay to not auto-pick-up when player dropped 1 arrow intentionally.
        /// </summary>
        public class GameManager_ItemDropServer
        {
            public static void Prefix(ItemStack _itemStack)
            {
                if (_itemStack.count == 1)
                {
                    if (_itemStack.itemValue?.ItemClass?.IsSticky == true)
                    {
                        var caller = Helper.GetCallerMethod();
                        //LogModWarning($"{caller.DeclaringType.Name}.{caller.Name}() : {_itemStack.itemValue?.ItemClass?.Name}");
                        if (caller.DeclaringType.Name == nameof(ItemActionEntryDrop) ||
                            (caller.DeclaringType.Name == nameof(XUiM_PlayerInventory) && caller.Name == nameof(XUiM_PlayerInventory.DropItem)))
                        {
                            XUiC_Toolbelt_Update_2.SkipTime = Time.time + 3; // 3 seconds
                        }
                    }
                }
            }
        }
    }
}
