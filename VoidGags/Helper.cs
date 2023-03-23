using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using static UIPopupList;

namespace VoidGags
{
    public static class Helper
    {
        /// <summary>
        /// Gets the configuration directory path for locked slots.
        /// </summary>
        public static string LockedSlotsConfigDirectory => VoidGags.ModFolder + "\\LockedSlots";

        /// <summary>
        /// Gets the local player entity.
        /// </summary>
        public static EntityPlayerLocal PlayerLocal => GameManager.Instance.World.GetPrimaryPlayer();

        /// <summary>
        /// Gets the current player identifier.
        /// </summary>
        public static string PlayerId => GameManager.Instance?.persistentLocalPlayer?.UserIdentifier?.ReadablePlatformUserIdentifier;

        /// <summary>
        /// Gets the current world seed.
        /// </summary>
        public static string WorldSeed => GameManager.Instance?.World?.Seed.ToString();

        /// <summary>
        /// Saves specified locked slots count to file.
        /// </summary>
        public static void SaveLockedSlotsCount(int lockedSlots)
        {
            var playerId = PlayerId;
            var worldSeed = WorldSeed;
            if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(worldSeed))
            {
                Directory.CreateDirectory(LockedSlotsConfigDirectory);
                File.WriteAllText(Path.Combine(LockedSlotsConfigDirectory, $"{playerId}-{worldSeed}.txt"), lockedSlots.ToString());
            }
        }

        /// <summary>
        /// Gets locked slots count from the corresponding file.
        /// </summary>
        public static int LoadLockedSlotsCount()
        {
            var playerId = PlayerId;
            var worldSeed = WorldSeed;
            if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(worldSeed))
            {
                var configFile = Path.Combine(LockedSlotsConfigDirectory, $"{playerId}-{worldSeed}.txt");
                if (File.Exists(configFile))
                {
                    return int.Parse(File.ReadAllText(configFile));
                }
            }
            return 0;
        }

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
        public static string GetCallStackPath()
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            var path = string.Join(" <-- ", stackTrace.GetFrames()
                .Skip(3)
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
            return entities.Where(e => (e.position - pos).magnitude < radius).Cast<TEntity>().ToList();
        }

        /// <summary>
        /// Waits for the function to return true.
        /// </summary>
        public static bool WaitFor(Func<bool> checkFunc, int checkIntervalMs = 2, int timeoutMs = 10000)
        {
            var startTime = DateTime.Now;
            while (!checkFunc())
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > timeoutMs)
                {
                    return false;
                }
                Task.Delay(checkIntervalMs).Wait();
            }
            return true;
        }

        /// <summary>
        /// Runs the specified action with a delay.
        /// </summary>
        public static Task DeferredAction(int delayMs, Action action)
        {
            return Task.Factory.StartNew(() =>
            {
                Task.Delay(delayMs).Wait();
                action();
            }, TaskCreationOptions.LongRunning);
        }
    }
}
