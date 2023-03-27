using System;
using UnityEngine;

namespace VoidGags
{
    public static class Extensions
    {
        public static bool Same(this string str, string another)
        {
            return str.Equals(another, StringComparison.CurrentCultureIgnoreCase);
        }

        public static XUiC_ItemStackGrid GetItemStackGrid(this XUiC_ContainerStandardControls controls)
        {
            var grid = controls.Parent?.Parent?.GetChildByType<XUiC_ItemStackGrid>();
            if (grid == null)
            {
                Debug.Log(new Exception($"Mod {nameof(VoidGags)}: Failed to retrieve 'XUiC_ItemStackGrid' from the 'XUiC_ContainerStandardControls'."));
            }
            return grid;
        }
    }
}
