namespace Core.Modifiers
{
    using System.Collections.Generic;
    using Entity.Components;
    using Enums;

    /// <summary>Draws pool entries into prefix/suffix slots: the pool splits into two weighted buckets
    /// by <see cref="IModifierDescriptor.Affix"/> and each bucket rolls its slot count independently
    /// (duplicates by entry are excluded, duplicate (parameter, type) across entries stay legal — variant B).
    /// A bucket short on entries honestly leaves the remaining slots empty (the item is born thinner) —
    /// reported as info, never an error. None entries must not reach a roll (strict parse guards it);
    /// one slipping through is skipped with an error.</summary>
    public static class AffixRoller
    {
        public static List<IModifierDescriptor> Roll(IReadOnlyCollection<IModifierDescriptor> pool, int prefixes, int suffixes, IRandomNumberGenerator rnd)
        {
            var prefixBucket = new List<IModifierDescriptor>();
            var suffixBucket = new List<IModifierDescriptor>();
            foreach (var descriptor in pool)
            {
                switch (descriptor.Affix)
                {
                    case AffixKind.Prefix: prefixBucket.Add(descriptor); break;
                    case AffixKind.Suffix: suffixBucket.Add(descriptor); break;
                    default: Tracker.TrackError($"Skipping pool entry without an affix — it can never occupy a slot: {descriptor}"); break;
                }
            }

            var result = new List<IModifierDescriptor>();
            RollBucket(prefixBucket, AffixKind.Prefix, prefixes, rnd, result);
            RollBucket(suffixBucket, AffixKind.Suffix, suffixes, rnd, result);
            return result;
        }

        private static void RollBucket(List<IModifierDescriptor> bucket, AffixKind kind, int slots, IRandomNumberGenerator rnd, List<IModifierDescriptor> result)
        {
            if (slots <= 0) return;

            (var weighted, float totalWeight) = WeightedRandomPicker.CalculateWeights(bucket);
            var picked = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(weighted, totalWeight, slots, rnd);
            if (picked.Count < slots)
                Tracker.TrackInfo($"{kind} bucket has only {picked.Count} of {slots} requested entries — the remaining slots stay empty");

            result.AddRange(picked);
        }
    }
}
