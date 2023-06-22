using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Platform;
using UnityEngine;

namespace VoidGags
{
    public static class Helper
    {
        /// <summary>
        /// Gets the local player entity.
        /// </summary>
        public static EntityPlayerLocal PlayerLocal => GameManager.Instance.World.GetPrimaryPlayer();

        /// <summary>
        /// Gets the current player identifier.
        /// </summary>
        public static string PlayerId => PlatformManager.InternalLocalUserIdentifier?.ReadablePlatformUserIdentifier;

        /// <summary>
        /// Gets the current world seed.
        /// </summary>
        public static string WorldSeed => GameManager.Instance?.World?.Seed.ToString();

        /// <summary>
        /// Gets the method the current method is called from.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static MethodBase GetCallerMethod(int index = 0)
        {
            index += 3;
            var stackTrace = new System.Diagnostics.StackTrace();
            if (stackTrace.FrameCount < index)
            {
                return null;
            }
            return stackTrace.GetFrame(index).GetMethod();
        }

        /// <summary>
        /// Gets call stack methods order.
        /// </summary>
        public static string GetCallStackPath(int limit = 5)
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            var path = string.Join(" <-- ", stackTrace.GetFrames()
                .Skip(3)
                .Take(limit)
                .Select(f => f.GetMethod())
                .Select(m => m.DeclaringType.Name + "." + m.Name + "()"));
            return path;
        }

        /// <summary>
        /// Creates new <see cref="GameRandom"/> instance for the specified coordinates.
        /// </summary>
        public static GameRandom GetRandomForPos(int x, int z)
        {
            GameRandom gameRandom = null;
            if (GameManager.Instance.World != null)
            {
                gameRandom = Utils.RandomFromSeedOnPos(x, z, GameManager.Instance.World.Seed);
            }
            return gameRandom;
        }

        /// <summary>
        /// Makes world position user frienrly in N/S/W/E coordinates + height.
        /// </summary>
        public static string WorldPosToCompasText(Vector3i p)
        {
            return (Math.Abs(p.x).ToString() + (p.x > 0 ? "E" : "W")) + ", " + (Math.Abs(p.z).ToString() + (p.z > 0 ? "N" : "S")) + ", " + p.y.ToString() + "h";
        }

        /// <summary>
        /// Calculates noise occlusion through the environment and obstacles.
        /// Method is based on the source code of <see cref="Audio.Manager.CalculateOcclusion(Vector3, Vector3)"/>
        /// </summary>
        public static float CalculateNoiseOcclusion(Vector3 positionOfSound, Vector3 positionOfEars, float distancePenalty)
        {
            Vector3 direction = positionOfSound - positionOfEars;
            float distance = direction.magnitude;
            if (distance < 1f)
            {
                return 1f;
            }

            float occ = 1f;
            if (Physics.Raycast(new Ray(positionOfEars - Origin.position, direction.normalized), out var hitInfo1, 50f, 65537) &&
                Physics.Raycast(new Ray(positionOfSound - Origin.position, (positionOfEars - positionOfSound).normalized), out var hitInfo2, 50f, 65537))
            {
                occ = Mathf.Max(distance - hitInfo2.distance - hitInfo1.distance, 0.2f);
                occ = 1f - (Mathf.Pow(Mathf.Clamp01(occ / 13f), 0.95f) * 0.9f);
            }

            return occ - (distance * distancePenalty);
        }

        /// <summary>
        /// Gets entities of specified type from the position.
        /// </summary>
        public static List<TEntity> GetEntities<TEntity>(Vector3 pos, float radius) where TEntity : Entity
        {
            var entities = new List<Entity>();
            Bounds bb = new Bounds(pos, new Vector3(radius + 1f, radius + 1f, radius + 1f) * 2f);
            GameManager.Instance.World.GetEntitiesInBounds(typeof(TEntity), bb, entities);
            if (entities.Count == 0)
            {
                return new List<TEntity>();
            }

            return entities.Select(e => ((e.position - pos).magnitude, e))
                .Where(x => x.magnitude < radius)
                .OrderBy(x => x.magnitude)
                .Select(x => x.e)
                .Cast<TEntity>()
                .ToList();
        }

        /// <summary>
        /// Gets the list of tile entities from the position.
        /// </summary>
        public static TileEntity[] GetTileEntities(Vector3 pos, float radius)
        {
            var tiles = new List<KeyValuePair<float, TileEntity>>();
            int maxDistance = Mathf.CeilToInt(radius);
            if (maxDistance >= 1)
            {
                var intPos = new Vector3i(pos);
                var world = GameManager.Instance.World;
                for (int x = -maxDistance; x <= maxDistance; x++)
                for (int y = -maxDistance; y <= maxDistance; y++)
                for (int z = -maxDistance; z <= maxDistance; z++)
                {
                    var tilePos = new Vector3i(intPos.x + x, intPos.y + y, intPos.z + z);
                    var distance = (pos - tilePos.ToVector3Center()).magnitude;
                    if (distance < radius)
                    {
                        var tile = world.GetTileEntity(0, tilePos);
                        if (tile != null)
                        {
                            tiles.Add(new KeyValuePair<float, TileEntity>(distance, tile));
                        }
                    }
                }
            }

            return tiles.OrderBy(e => e.Key).Select(e => e.Value).Distinct().ToArray();
        }

        /// <summary>
        /// Performs the specified action with a delay.
        /// </summary>
        public static void DeferredAction(float seconds, Action action)
        {
            if (seconds <= 0f)
            {
                action();
                return;
            }

            GameManager.Instance.StartCoroutine(Job());

            IEnumerator Job()
            {
                yield return new WaitForSeconds(seconds);
                action();
            }
        }

        /// <summary>
        /// Performs the specified action once condition is met.
        /// </summary>
        public static void DoWhen(Action action, Func<bool> condition, float checkInterval = 0.02f, float timeout = 3f, Func<bool> failureCondition = null, Action failureAction = null)
        {
            var endTime = Time.time + timeout;
            GameManager.Instance.StartCoroutine(Job());

            IEnumerator Job()
            {
                while (true)
                {
                    if (condition())
                    {
                        action();
                        break;
                    }
                    if (failureCondition != null && failureCondition())
                    {
                        failureAction?.Invoke();
                        break;
                    }
                    if (Time.time > endTime)
                    {
                        failureAction?.Invoke();
                        break;
                    }
                    yield return new WaitForSeconds(checkInterval);
                }
            }
        }

        /// <summary>
        /// Checks whether specified position is in the trader area.
        /// </summary>
        public static bool IsTraderArea(Vector3i pos)
        {
            return GameManager.Instance.World.IsWithinTraderArea(pos);
        }
    }
}
