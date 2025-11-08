using System;
using UnityEngine;

namespace VaultSystems.Quests
{
    [Serializable]
    public class SubquestProgress
    {
        public string key;
        public int current;
        public int target;
        public bool IsComplete => current >= target;

        public void Increment(int amount = 1)
        {
            current += amount;
            if (current > target) current = target;
        }
    }
}
