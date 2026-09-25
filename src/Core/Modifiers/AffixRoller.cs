namespace Core.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using Entity.Components;
    using Enums;

    /// <summary>Draws pool entries into prefix/suffix slots: the pool splits into two weighted buckets
    /// by <see cref="IModifierDescriptor.Affix"/> and each bucket rolls its slot count, both sharing ONE
    /// set of taken identities — an item never wears the same <see cref="LineIdentity"/> twice, whichever
    /// family it came from. Composites carry a whole-set identity: same part set = same line (tier entries
    /// from different sources compete like atoms), while a multi-part bundle still neither blocks nor is
    /// blocked by its parts' standalone atoms. A bucket short on legal entries honestly leaves the
    /// remaining slots empty (the item is born thinner) — reported as info, never an error. None entries
    /// must not reach a roll (strict parse guards it); one slipping through is skipped with an error.</summary>
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
                    // A mythic entry belongs to the item's own slot (ascension draws it) and never competes
                    // for a prefix/suffix — passing through a generation pool is a data slip, not a crash.
                    case AffixKind.Mythic: Tracker.TrackInfo($"Skipping mythic pool entry in an affix roll — it belongs to the mythic slot: {descriptor}"); break;
                    default: Tracker.TrackError($"Skipping pool entry without an affix — it can never occupy a slot: {descriptor}"); break;
                }
            }

            var result = new List<IModifierDescriptor>();
            var taken = new HashSet<object>();
            RollBucket(prefixBucket, AffixKind.Prefix, prefixes, rnd, result, taken);
            RollBucket(suffixBucket, AffixKind.Suffix, suffixes, rnd, result, taken);
            return result;
        }

        // One slot at a time: entries whose identity is already taken drop out and the weights are recomputed
        // over what is still legal, so the odds stay proportional among the remaining entries and a rejected
        // entry never burns a slot.
        private static void RollBucket(List<IModifierDescriptor> bucket, AffixKind kind, int slots, IRandomNumberGenerator rnd, List<IModifierDescriptor> result, HashSet<object> taken)
        {
            for (int slot = 0; slot < slots; slot++)
            {
                var legal = bucket.Where(descriptor => !taken.Contains(RollIdentity(descriptor))).ToList();
                (var weighted, float totalWeight) = WeightedRandomPicker.CalculateWeights(legal);
                if (totalWeight <= 0f)
                {
                    Tracker.TrackInfo($"{kind} bucket has only {slot} of {slots} pickable entries — the remaining slots stay empty");
                    return;
                }

                var picked = WeightedRandomPicker.PickRandom(weighted, totalWeight, rnd);
                taken.Add(RollIdentity(picked));
                result.Add(picked);
            }
        }

        /// <summary>What makes two picks "the same line" for slot purposes: the line identity when the entry
        /// has one (atoms and composites alike — see <see cref="LineIdentity"/>), the entry itself otherwise
        /// (grant/operation entries dedup only as literal repeats; they are not lines).</summary>
        private static object RollIdentity(IModifierDescriptor descriptor) =>
            LineIdentity.TryFrom(descriptor, out var identity) ? identity : descriptor;
    }
}
