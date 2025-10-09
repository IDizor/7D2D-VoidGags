using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoidGags.Types
{
    public class DelayStorage
    {
        private float delay = 1f;
        private float time = 0f;
        private Dictionary<int, float> acc = null;

        public int Counter = 0;

        public DelayStorage(float delay, Dictionary<int, float> acceleration = null)
        {
            this.delay = delay;
            acc = acceleration;
        }

        public bool Check()
        {
            var d = delay;
            if (acc != null)
            {
                var key = acc.Keys.Max(k => k <= Counter ? k : -1);
                if (key >= 0)
                {
                    d = acc[key];
                }
            }

            if (Time.time - time > d)
            {
                time = Time.time;
                Counter++;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            time = 0f;
            Counter = 0;
        }
    }

    public class DelayStorage<T>
    {
        private const float clearThreshold = 600f; // 10 minutes
        private float clearTimeStamp = 0f;
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
                // save record
                records[key] = Time.time;

                // clear old records
                if (Time.time - clearTimeStamp > clearThreshold)
                {
                    clearTimeStamp = Time.time;
                    records.RemoveAll((time) => Time.time - time > clearThreshold);
                }
                
                return true;
            }
            return false;
        }

        public void Reset()
        {
            records.Clear();
        }
    }
}
