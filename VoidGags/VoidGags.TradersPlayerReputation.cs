namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_TradersPlayerReputation()
        {
            LogApplyingPatch(nameof(Settings.TradersPlayerReputation));
            UseXmlPatches(nameof(Settings.TradersPlayerReputation));
        }
    }
}
