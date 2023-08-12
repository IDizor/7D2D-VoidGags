using UnityEngine.Scripting;

namespace VoidGags.NetPackages
{
    [Preserve]
    public class NetPackageMasterWorkChance : NetPackage
    {
        private float chance;

        public NetPackageMasterWorkChance Setup(float _chance)
        {
            chance = _chance;
            return this;
        }

        public override void read(PooledBinaryReader _br)
        {
            chance = _br.ReadSingle();
        }

        public override void write(PooledBinaryWriter _bw)
        {
            base.write(_bw);
            _bw.Write(chance);
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
            {
                // send own value to clients
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageMasterWorkChance>().Setup(VoidGags.MasterWorkChanceValue));
            }
            else
            {
                // apply chance
                if (Settings.MasterWorkChance > 0)
                {
                    if (VoidGags.MasterWorkChanceValue != chance)
                    {
                        VoidGags.MasterWorkChanceValue = chance;
                        VoidGags.LogModWarning($"Master Work chance value is changed by the Server to {chance * 100:0.00} percent.");
                    }
                }
            }
        }

        public override int GetLength()
        {
            return 4;
        }
    }
}
