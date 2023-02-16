using System.IO;
using System.Linq;
using System.Reflection;

namespace VoidGags
{
    public static class Helper
    {
        /// <summary>
        /// Gets the configuration directory path for locked slots.
        /// </summary>
        public static string LockedSlotsConfigDirectory => Path.GetDirectoryName(Assembly.GetAssembly(typeof(VoidGags)).Location) + "\\LockedSlots";

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
    }
}
