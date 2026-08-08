namespace Core.PassiveTree.Rules
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Data.GameData;
    using Newtonsoft.Json;

    /// <summary>
    /// Reads the tree's pricing through the ordinary data seam and answers it. Its own catalog rather
    /// than a second file beside the tree: the tree's reader parses every file it is handed as a
    /// document, so a pricing file in that folder would be read as a tree with no nodes.
    /// </summary>
    public sealed class PassiveTreeRulesProvider : IPassiveRespecPricing, IGameDataParticipant
    {
        private PassiveTreeRulesData _rules = new();

        public IReadOnlyList<string> Catalogs => [DataCatalog.PassiveTreeRules];

        public void Apply(string catalog, GameDataFile file)
        {
            var parsed = JsonConvert.DeserializeObject<PassiveTreeRulesData>(file.Json)
                         ?? throw new InvalidOperationException($"Failed to deserialize passive tree rules '{file.FileName}'");

            _rules = Sanitized(parsed, file.FileName);
        }

        /// <summary>
        /// The price of one respec: gold per node, opened up by the character's own progress, and cut to
        /// the ceiling. Rounded UP, so the last fraction of a node is paid for rather than given away.
        /// <para>A count that is not positive is not a respec and costs nothing — the gate refuses an
        /// empty set before this is ever asked, and a price of zero is the honest answer either way.</para>
        /// </summary>
        public int PriceOf(int nodeCount, int masteryLevel)
        {
            if (nodeCount <= 0) return 0;

            RespecPricingData respec = _rules.Respec;
            float scaled = nodeCount * respec.GoldPerNode * (1f + respec.MasteryScale * Math.Max(0, masteryLevel));

            return Math.Min(respec.MaxCost, (int)MathF.Ceiling(scaled));
        }

        /// <summary>A price that is not positive is reported and replaced by the shipped default: a zero
        /// per node or a zero ceiling would make every respec free, which is the one thing the price
        /// exists to prevent, and it would do it silently.</summary>
        private static PassiveTreeRulesData Sanitized(PassiveTreeRulesData parsed, string fileName)
        {
            var defaults = new RespecPricingData();

            return parsed with
            {
                Respec = parsed.Respec with
                {
                    GoldPerNode = Positive(parsed.Respec.GoldPerNode, defaults.GoldPerNode, "respec.goldPerNode", fileName),
                    MaxCost = Positive(parsed.Respec.MaxCost, defaults.MaxCost, "respec.maxCost", fileName),

                    // A flat price is a legal design; a negative one would pay the player for undoing his
                    // own plan, which is a farm and not a discount.
                    MasteryScale = NotNegative(parsed.Respec.MasteryScale, defaults.MasteryScale, "respec.masteryScale", fileName)
                }
            };
        }

        private static T Positive<T>(T value, T fallback, string field, string fileName) where T : INumber<T> =>
            value > T.Zero ? value : Reported(value, fallback, field, fileName, "positive");

        private static T NotNegative<T>(T value, T fallback, string field, string fileName) where T : INumber<T> =>
            value >= T.Zero ? value : Reported(value, fallback, field, fileName, "zero or more");

        private static T Reported<T>(T value, T fallback, string field, string fileName, string expectation) where T : INumber<T>
        {
            Tracker.TrackError($"Passive tree rules '{fileName}': '{field}' must be {expectation} but is {value}, falling back to {fallback}");
            return fallback;
        }
    }
}
