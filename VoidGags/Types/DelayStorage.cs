using System.Collections.Concurrent;
using UnityEngine;

namespace VoidGags.Types
{
    public class DelayStorage<T>
    {
        private const float clearThreshold = 600f;
        private float clearTime = Time.time;
        private float delay = 1f;
        private ConcurrentDictionary<T, float> records = new();

        public DelayStorage(float delay)
        {
            this.delay = delay;
        }

        public bool Check(T key)
        {
            if (!records.TryGetValue(key, out float time) || Time.time - time > delay)
            {
                // clear old records
                if (Time.time - clearTime > clearThreshold)
                {
                    records.RemoveAll((time) => Time.time - time > clearThreshold);
                }

                // save new record
                records[key] = Time.time;
                return true;
            }
            return false;
        }
    }
}
