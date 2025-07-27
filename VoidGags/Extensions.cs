using System;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace VoidGags
{
    public static class Extensions
    {
        public static string Serialize(this object o)
        {
            return JsonConvert.SerializeObject(o);
        }

        public static TValue GetFieldValue<TValue>(this object obj, string name)
        {
            return (TValue)AccessTools.Field(obj.GetType(), name).GetValue(obj);
        }

        public static bool Same(this string str, string another)
        {
            return str.Equals(another, StringComparison.CurrentCultureIgnoreCase);
        }

        public static XUiC_ItemStackGrid GetItemStackGrid(this XUiC_ContainerStandardControls controls)
        {
            var grid = controls.Parent?.Parent?.GetChildByType<XUiC_ItemStackGrid>();
            if (grid == null)
            {
                VoidGags.LogModException("Failed to find 'XUiC_ItemStackGrid' from the 'XUiC_ContainerStandardControls'.");
            }
            return grid;
        }

        public static bool IsBusy(this EntityAlive entity)
        {
            return IsInvestigating(entity) || IsAttacking(entity);
        }

        public static bool IsAttacking(this EntityAlive entity)
        {
            return entity.GetAttackTarget() != null && entity.attackTargetTime > 0;
        }

        public static bool IsInvestigating(this EntityAlive entity)
        {
            return entity.HasInvestigatePosition && entity.InvestigatePosition != Vector3.zero && entity.isInvestigateAlert;
        }

        public static bool InvestigatesMoreDistantPos(this EntityAlive entity, Vector3 thanPos)
        {
            if (entity.HasInvestigatePosition && entity.InvestigatePosition != Vector3.zero)
            {
                var currentDistance = (entity.position - entity.InvestigatePosition).magnitude;
                var thanDistance = (entity.position - thanPos).magnitude;

                return currentDistance > thanDistance;
            }

            return false;
        }

        public static bool CanMove(this EntityAlive entity)
        {
            return entity != null
                && entity.IsDead() == false
                && entity.emodel != null
                && entity.emodel.IsRagdollActive == false
                && entity.Electrocuted == false
                && entity.bodyDamage.CurrentStun != EnumEntityStunType.Kneel
                && entity.bodyDamage.CurrentStun != EnumEntityStunType.Prone
                && entity.bodyDamage.CurrentStun != EnumEntityStunType.Getup;
        }

        public static bool ContainsAnyItem(this ITileEntityLootable container, XUiC_ItemStackGrid itemGrid)
        {
            if (itemGrid == null || container == null)
            {
                return false;
            }

            XUiController[] itemStackControllers = itemGrid.GetItemStackControllers();

            for (int i = 0; i < itemStackControllers.Length; i++)
            {
                var xUiC_ItemStack = (XUiC_ItemStack)itemStackControllers[i];
                if (!xUiC_ItemStack.StackLock && !xUiC_ItemStack.UserLockedSlot)
                {
                    var itemStack = xUiC_ItemStack.ItemStack;
                    if (!itemStack.IsEmpty() && container.HasItem(itemStack.itemValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static int GetCurrentHP(this BlockValue block)
        {
            return block.Block.MaxDamage - block.damage;
        }

        public static Vector3i Invert(this Vector3i vector)
        {
            return vector * -1;
        }

        public static string GetVehicleEntityId(this XUiC_ContainerStandardControls controls)
        {
            if (controls.xui.vehicle != null)
            {
                return $"vehicle-{controls.xui.vehicle.EntityId}";
            }
            return null;
        }

        public static bool HasFuelAndCanStart(this XUiC_WorkstationFuelGrid workstation)
        {
            return !workstation.WorkstationData.GetIsBesideWater()
                && (workstation.hasAnyFuel() || workstation.WorkstationData.GetBurnTimeLeft() > 0f);
        }

        public static bool GiveItem(this EntityPlayerLocal player, ItemStack item)
        {
            if (!player.playerUI.xui.PlayerInventory.AddItem(item))
            {
                player.playerUI.xui.PlayerInventory.DropItem(item);
                return false;
            }
            return true;
        }

        public static List<TEntity> GetEntities<TEntity>(this Chunk chunk, Vector3 pos, float distance) where TEntity : Entity
        {
            var entities = new List<TEntity>();
            if (chunk.entityLists?.Length > 0)
            {
                foreach (var list in chunk.entityLists)
                {
                    if (list?.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            if (item is TEntity entity && (pos - item.position).magnitude <= distance)
                            {
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
            return entities;
        }

        public static bool Get(this PackedBoolArray array, int index)
        {
            if (array == null || array.Length <= index)
                return false;

            return array[index];
        }

        public static bool IsBackpack(this XUiC_ContainerStandardControls controls)
        {
            return controls?.Parent?.Parent?.GetType() == typeof(XUiC_BackpackWindow);
        }

        public static ProgressionValue PerkSalvageOperations(this EntityPlayer player)
        {
            return player.Progression.GetProgressionValue("perkSalvageOperations");
        }

        public static ProgressionValue PerkParkour(this EntityPlayer player)
        {
            return player.Progression.GetProgressionValue("perkParkour");
        }
    }
}
