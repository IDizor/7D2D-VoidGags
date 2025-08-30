using UnityEngine.Scripting;

namespace VoidGags.NetPackages
{
    [Preserve]
    public class NetPackageMasterWorkCreated : NetPackage
    {
        private int playerId;

        public NetPackageMasterWorkCreated Setup(int _playerId)
        {
            playerId = _playerId;
            return this;
        }

        public override void read(PooledBinaryReader _br)
        {
            playerId = _br.ReadInt32();
        }

        public override void write(PooledBinaryWriter _bw)
        {
            base.write(_bw);
            //_bw.Write(playerId);
            PooledBinaryWriter_Methods.WriteInt.Invoke(_bw, [playerId]);
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            var localPlayer = _world.GetPrimaryPlayer();
            if (localPlayer != null && localPlayer.entityId == playerId)
            {
                VoidGags.MasterWorkChance.PlayMasterWorkSound();
            }
        }

        public override int GetLength()
        {
            return 4;
        }
    }
}
