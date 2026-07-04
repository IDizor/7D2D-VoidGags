namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        public void ApplyPatches_NewTradersDontKnowYou()
        {
            LogApplyingPatch(nameof(Settings.NewTradersDontKnowYou));
            UseXmlPatches(nameof(Settings.NewTradersDontKnowYou));
        }
    }
}
