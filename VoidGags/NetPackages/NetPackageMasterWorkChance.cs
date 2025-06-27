using UnityEngine.Scripting;

namespace VoidGags.NetPackages
{
    [Preserve]
    public class NetPackageMasterWorkChance : NetPackage
    {
        private float chance;
        private int maxQuality;

        public NetPackageMasterWorkChance Setup(float _chance, int _maxQuality)
        {
            chance = _chance;
            maxQuality = _maxQuality;
            return this;
        }

        public override void read(PooledBinaryReader _br)
        {
            chance = _br.ReadSingle();
            maxQuality = _br.ReadInt32();
        }

        public override void write(PooledBinaryWriter _bw)
        {
            base.write(_bw);
            //_bw.Write(chance);
            //_bw.Write(maxQuality);
            PooledBinaryWriter_Methods.WriteFloat.Invoke(_bw, [chance]);
            PooledBinaryWriter_Methods.WriteInt.Invoke(_bw, [maxQuality]);
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
            {
                // send own values to clients
                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageMasterWorkChance>()
                    .Setup(VoidGags.MasterWorkChanceValue, Settings.MasterWorkChance_MaxQuality));
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

                // apply max quality
                if (Settings.MasterWorkChance_MaxQuality != maxQuality)
                {
                    Settings.MasterWorkChance_MaxQuality = maxQuality;
                    VoidGags.LogModWarning($"Master Work max quality is changed by the Server to {maxQuality}.");
                }
            }
        }

        public override int GetLength()
        {
            return 8;
        }
    }
}
