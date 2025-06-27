using System.Reflection;

namespace VoidGags.NetPackages
{
    public static class PooledBinaryWriter_Methods
    {
        public static MethodInfo WriteFloat = typeof(PooledBinaryWriter).GetMethod("Write", [typeof(float)]);
        public static MethodInfo WriteInt = typeof(PooledBinaryWriter).GetMethod("Write", [typeof(int)]);
    }
}
