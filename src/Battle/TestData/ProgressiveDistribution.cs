namespace Battle.TestData
{
    using Godot;
    using System;
    using System.Collections.Generic;

    public abstract class ProgressiveDistribution<TKey>(float[] baseChances, float[] targetChances)
        where TKey : struct, Enum
    {
        private readonly float[] BaseChances = baseChances;
        private readonly float[] TargetChances = targetChances;
        private readonly Dictionary<TKey, (float BaseChance, float TargetChance)> _chances = [];

        public void AddChance(TKey key, float baseChance, float targetChance)
        {
            _chances.TryAdd(key, (baseChance, targetChance));
        }

        public float GetProbabilityForKey(TKey key, float progress, float bonus = 0)
        {
            _chances.TryGetValue(key, out var value);
            float chance = Mathf.Lerp(value.BaseChance, value.TargetChance, progress);
            chance *= 1f + bonus;
            return chance;
        }
    }
}
