namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_HighlightLockedSlots()
        {
            LogApplyingPatch(nameof(Settings.HighlightLockedSlots));

            if (IsUndeadLegacy)
            {
                LogModWarning($"Patch '{nameof(Settings.HighlightLockedSlots)}' is not compatible with Undead Legacy.");
                return;
            }

            UseXmlPatches(nameof(Settings.HighlightLockedSlots));
        }
    }
}
