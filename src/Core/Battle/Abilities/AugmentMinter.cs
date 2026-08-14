namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Data.AbilityData;
    using Entity.Components;

    /// <summary>
    /// Where an augment copy is born. The record says what the augment is and what its numbers are
    /// worth on average; the copy is one draw around those numbers, taken here and kept for good.
    ///
    /// The draw goes through the project's own generator interface. Never Godot's class directly: an
    /// engine generator is a native object, and a composition built outside the runtime takes the
    /// process down the moment one is constructed.
    /// </summary>
    /// <param name="augments">What an id means — the records the numbers are rolled around.</param>
    /// <param name="rules">Where the spread comes from. Read at every mint rather than once, so a
    /// reloaded rules file is worth something without rebuilding the minter.</param>
    public sealed class AugmentMinter(
        IAbilityAugmentCatalog augments,
        ICombatRulesProvider rules,
        IRandomNumberGenerator rnd)
    {
        /// <summary>A copy of the augment named. Null for an id no record declares — there is nothing
        /// to roll around, and a copy with no numbers would be an augment that does nothing.</summary>
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
        public AugmentInstance Mint(AbilityAugmentData record, Enums.Rarity rarity) =>
            new(record.Id, Rolled(record), rarity);

        /// <summary>One draw from the record's band, uniform across the steps it spans. Uniform is a
        /// PLACEHOLDER — the curve is a balance decision (see Docs/PLAN-Augments.md §4e).</summary>
        public Enums.Rarity RollRarity(AbilityAugmentData record)
        {
            (Enums.Rarity worst, Enums.Rarity best) = record.RarityBand;
            if (worst == best) return worst;

            // The scale runs downward (Legendary is zero), so the band spans from the best value up.
            int from = (int)best;
            int to = (int)worst;
            return (Enums.Rarity)(from <= to ? rnd.RandIntRange(from, to) : rnd.RandIntRange(to, from));
        }

        /// <summary>Every number the record declares, drawn once each.</summary>
        private Dictionary<string, float> Rolled(AbilityAugmentData record)
        {
            AugmentValueRules values = rules.AugmentValues;
            Dictionary<string, float> rolled = new(record.UpgradeProperties.Count);

            foreach ((string property, float declared) in record.UpgradeProperties)
                rolled[property] = values.Roll(declared, rnd);

            return rolled;
        }
    }
}
