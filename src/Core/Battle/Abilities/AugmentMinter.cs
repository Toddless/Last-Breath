namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Data.AbilityData;
    using Entity.Components;
    using Enums;

    /// <summary>
    /// Where an augment copy is born. The record says what the augment is worth at each rarity; the
    /// copy draws ONE thing — where it landed on the rarity scale — and its numbers follow from that
    /// exactly. Two copies of one record at one rarity are the same augment.
    ///
    /// The draw goes through the project's own generator interface. Never Godot's class directly: an
    /// engine generator is a native object, and a composition built outside the runtime takes the
    /// process down the moment one is constructed.
    /// </summary>
    /// <param name="augments">What an id means — the records the rungs are read off.</param>
    public sealed class AugmentMinter(IAbilityAugmentCatalog augments, IRandomNumberGenerator rnd)
    {
        /// <summary>A copy of the augment named. Null for an id no record declares — there are no rungs
        /// to read, and a copy with no numbers would be an augment that does nothing.</summary>
        public AugmentInstance? Mint(string augmentId)
        {
            AbilityAugmentData? record = augments.Find(augmentId);
            if (record != null) return Mint(record);

            Tracker.TrackNotFound($"Augment record '{augmentId}'", this);
            return null;
        }

        /// <summary>A copy of a record already in hand, rarity drawn from its own band.</summary>
        public AugmentInstance Mint(AbilityAugmentData record) => Mint(record, RollRarity(record));

        /// <summary>A copy at a rarity somebody else decided — a loot table position that says what the
        /// seat is worth. The numbers are still this copy's own draw.</summary>
        public AugmentInstance Mint(AbilityAugmentData record, Rarity rarity) =>
            new(record.Id, Rolled(record, rarity), rarity, RollEffect(record));

        /// <summary>Which effect of the record's pool this copy will lay — the second and last thing luck
        /// decides about a copy, drawn on the same seam as the rarity. Empty for a record that names its
        /// effect itself, which is what most of the catalog does.</summary>
        public string RollEffect(AbilityAugmentData record)
        {
            IReadOnlyList<string> pool = record.PoolEffects;
            return pool.Count == 0 ? string.Empty : pool[rnd.RandIntRange(0, pool.Count - 1)];
        }

        /// <summary>One draw from the record's band, uniform across the steps it spans. Uniform is a
        /// PLACEHOLDER — the curve is a balance decision (see Docs/PLAN-Augments.md §4e).</summary>
        public Rarity RollRarity(AbilityAugmentData record)
        {
            (Rarity worst, Rarity best) = record.RarityBand;
            if (worst == best) return worst;

            // The scale runs downward (Legendary is zero), so the band spans from the best value up.
            int from = (int)best;
            int to = (int)worst;
            return (Rarity)(from <= to ? rnd.RandIntRange(from, to) : rnd.RandIntRange(to, from));
        }

        /// <summary>
        /// Every number the record declares, at the rung this copy's rarity stands on
        /// (<see cref="AugmentValueLadder"/>). Nothing is drawn here: a copy's numbers follow from its
        /// rarity exactly, so two copies of one record at one rarity are the same augment and the only
        /// thing luck decides about a copy is where it landed on the scale.
        /// </summary>
        private static Dictionary<string, float> Rolled(AbilityAugmentData record, Rarity rarity)
        {
            Dictionary<string, float> values = new(record.UpgradeProperties.Count);

            foreach (string property in record.UpgradeProperties.Keys)
            {
                IReadOnlyList<float>? authored = record.AuthoredRungs(property);
                if (authored != null)
                {
                    values[property] = AugmentValueLadder.Rung(authored, record.RarityBand, rarity);
                    continue;
                }

                (float atWorst, float atBest) = record.Ends(property);
                values[property] = AugmentValueLadder.Rung(atWorst, atBest, record.RarityBand, rarity);
            }

            return values;
        }
    }
}
