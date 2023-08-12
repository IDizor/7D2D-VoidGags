using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace VoidGags
{
    public static class Extensions
    {
        private static FieldInfo attackTargetTime = AccessTools.Field(typeof(EntityAlive), "attackTargetTime"); // int
        private static FieldInfo isInvestigateAlert = AccessTools.Field(typeof(EntityAlive), "isInvestigateAlert"); // bool

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
            return entity.GetAttackTarget() != null && (int)attackTargetTime.GetValue(entity) > 0;
        }

        public static bool IsInvestigating(this EntityAlive entity)
        {
            return entity.HasInvestigatePosition && entity.InvestigatePosition != Vector3.zero && (bool)isInvestigateAlert.GetValue(entity);
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

        public static bool ContainsAnyItem(this TileEntityLootContainer container, XUiC_ItemStackGrid itemGrid, int lockedSlots)
        {
            if (itemGrid == null || container == null)
            {
                return false;
            }

            XUiController[] itemStackControllers = itemGrid.GetItemStackControllers();

            for (int i = lockedSlots; i < itemStackControllers.Length; i++)
            {
                var xUiC_ItemStack = (XUiC_ItemStack)itemStackControllers[i];
                if (!xUiC_ItemStack.StackLock)
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
    }
}
