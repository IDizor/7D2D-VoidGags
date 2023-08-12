using UnityEngine;
using UnityEngine.Scripting;

namespace VoidGags.NetPackages
{
    [Preserve]
    public class NetPackageSetInvestigatePos : NetPackage
    {
        private int entityId;
        private Vector3 pos;
        private int ticks;

        public NetPackageSetInvestigatePos Setup(int _entityId, Vector3 _pos, int _ticks)
        {
            entityId = _entityId;
            pos = _pos;
            ticks = _ticks;
            return this;
        }

        public override void read(PooledBinaryReader _br)
        {
            entityId = _br.ReadInt32();
            pos = new Vector3(_br.ReadSingle(), _br.ReadSingle(), _br.ReadSingle());
            ticks = _br.ReadInt32();
        }

        public override void write(PooledBinaryWriter _bw)
        {
            base.write(_bw);
            _bw.Write(entityId);
            _bw.Write(pos.x);
            _bw.Write(pos.y);
            _bw.Write(pos.z);
            _bw.Write(ticks);
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            var world = GameManager.Instance?.World;
            var entityAlive = (EntityAlive)world?.GetEntity(entityId);
            if (entityAlive != null && !entityAlive.IsDead())
            {
                entityAlive.ConditionalTriggerSleeperWakeUp();
                entityAlive.SetInvestigatePosition(pos, ticks, isAlert: true);
            }
        }

        public override int GetLength()
        {
            return 4 + (4 * 3) + 4;
        }
    }
}
