using System.Collections.Generic;
using UnityEngine;

namespace VaultSystems.Quests
{
    /// <summary>
    /// Container for managing multiple subquest progress trackers.
    /// </summary>
    public class QuestProgressContainer
    {
        private Dictionary<string, SubquestProgress> progressDict = new();

        /// <summary>
        /// Get or create a progress tracker for the given key.
        /// </summary>
        public SubquestProgress GetOrCreate(string key, int target)
        {
            if (!progressDict.TryGetValue(key, out var prog))
            {
                prog = new SubquestProgress { key = key, target = target };
                progressDict[key] = prog;
            }
            return prog;
        }

        /// <summary>
        /// Get all progress trackers.
        /// </summary>
        public IEnumerable<SubquestProgress> GetAllProgress()
        {
            return progressDict.Values;
        }

        /// <summary>
        /// Clear all progress trackers.
        /// </summary>
        public void Clear()
        {
            progressDict.Clear();
        }
    }
}
