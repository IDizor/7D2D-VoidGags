using System;
using System.Linq;
using System.Reflection;

namespace VoidGags
{
    /// <summary>
    /// 7 Days To Die game modification.
    /// </summary>
    public partial class VoidGags : IModApi
    {
        private Assembly UndeadLegacyAssembly = null;
        private static bool IsUndeadLegacy = false;

        private void CheckUndeadLegacy()
        {
            UndeadLegacyAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.FullName.StartsWith("UndeadLegacy,"));

            IsUndeadLegacy = UndeadLegacyAssembly != null;
        }
    }
}
