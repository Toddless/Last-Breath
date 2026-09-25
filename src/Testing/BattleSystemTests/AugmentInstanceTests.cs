namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity.Components;
    using Core.Enums;

    /// <summary>
    /// An augment is a copy of its record, not the record itself — but since CL-5 the only thing a copy
    /// draws is WHERE IT LANDED ON THE RARITY SCALE. Its numbers follow from that rarity exactly, off
    /// the ladder its design line describes, so two copies of one record at one rarity are the same
    /// augment and the id plus the rarity is the whole of what a copy is.
    ///
    /// The ±25% draw around the numbers is gone by the owner's decision. What is walked here is what
    /// replaced it: the copy is worth its rung and nothing else, the rarity is still genuinely drawn
    /// from the record's band, and no number of any shipped record comes out as nothing — a count of
    /// zero turns is an augment chosen, worn, paid for and silently doing nothing.
    /// </summary>
    [TestClass]
    public class AugmentInstanceTests
    {
        /// <summary>A record of the shipped catalog whose numbers are shares and whose band spans
        /// several rarities, so its rungs differ from one another.</summary>
        private const string ShareAugment = "Augment_Reduce_Cost";

        private const string ShareProperty = "costShare";

        /// <summary>A shipped record whose effect is drawn rather than named — the second and last thing
        /// luck decides about a copy.</summary>
        private const string PoolAugment = "Augment_Apply_Debuff";

        [TestMethod]
        public void ACopyIsWorthExactlyTheRungOfItsRarity()
        {
            // The whole of the new rule. No band, no draw: name the rarity and the number follows.
            AbilityAugmentData record = ShippedRecord(ShareAugment);
            (float atWorst, float atBest) = record.Ends(ShareProperty);
            AugmentMinter minter = MinterOver(new DefaultRandomNumberGenerator(seed: 20260815));

            for (int step = (int)record.RarityBand.Worst; step >= (int)record.RarityBand.Best; step--)
            {
                var rarity = (Rarity)step;
                float expected = AugmentValueLadder.Rung(atWorst, atBest, record.RarityBand, rarity);

                Assert.AreEqual(expected, minter.Mint(record, rarity).Values[ShareProperty], 0.0001f,
                    $"a {rarity} copy is not worth its own rung");
            }
        }

        [TestMethod]
        public void TwoCopiesOfOneRecordDifferOnlyWhenTheirRarityDoes()
        {
            // Both halves of what a copy now is. Same rarity, same augment — nothing left to separate
            // them. Different rarity, different worth — which is what the rarity is FOR.
            AbilityAugmentData record = ShippedRecord(ShareAugment);
            AugmentMinter minter = MinterOver(new DefaultRandomNumberGenerator(seed: 7));

            float first = minter.Mint(record, Rarity.Legendary).Values[ShareProperty];
            float second = minter.Mint(record, Rarity.Legendary).Values[ShareProperty];
            float plainer = minter.Mint(record, record.RarityBand.Worst).Values[ShareProperty];

            Assert.AreEqual(first, second, 0.0001f, "two copies of one record at one rarity came out different");
            Assert.AreNotEqual(first, plainer, "the worst and the best rarity of a laddered record are worth the same");
        }

        [TestMethod]
        public void MintingSpendsADrawOnTheRarityAndOnNothingElse()
        {
            // The generator is shared, so a draw spent here shifts every seeded sequence taken after it.
            // One draw for the rarity when the band has width; none at all when the rarity is handed in.
            AbilityAugmentData record = ShippedRecord(ShareAugment);

            var counted = new CountingRandom(new DefaultRandomNumberGenerator(seed: 3));
            new AugmentMinter(ShippedAbilityData.Augments(), counted).Mint(record, Rarity.Legendary);
            Assert.AreEqual(0, counted.Draws, "minting at a named rarity still spent a draw");

            var drawn = new CountingRandom(new DefaultRandomNumberGenerator(seed: 3));
            new AugmentMinter(ShippedAbilityData.Augments(), drawn).Mint(record);
            Assert.AreEqual(1, drawn.Draws, "minting drew something other than the rarity alone");

            // A pool record has a second thing to decide — which of its effects this copy lays — and
            // exactly one more draw is what deciding it costs. Both shipped pool records stand on one
            // rarity, so their band costs nothing and the single draw counted here IS the effect.
            AbilityAugmentData pooled = ShippedAbilityData.Augments().Find(PoolAugment)!;
            Assert.IsTrue(pooled.PoolEffects.Count > 1, $"'{PoolAugment}' offers nothing to choose between");
            Assert.AreEqual(pooled.RarityBand.Worst, pooled.RarityBand.Best, $"'{PoolAugment}' spans rarities, so the count below is two things at once");

            var pooledDrawn = new CountingRandom(new DefaultRandomNumberGenerator(seed: 3));
            new AugmentMinter(ShippedAbilityData.Augments(), pooledDrawn).Mint(pooled);
            Assert.AreEqual(1, pooledDrawn.Draws, "a pool copy drew something other than its effect");
        }

        [TestMethod]
        public void EveryCopyLandsInsideItsRecordsBandAndAWideBandDoesNotAlwaysAnswerTheSame()
        {
            // The rarity is the one thing still drawn. Two ways for it to go wrong quietly: a draw that
            // leaves the band the record declares, and a "draw" that always answers the same — an
            // authored rarity wearing a band's clothes.
            AugmentMinter minter = MinterOver(new DefaultRandomNumberGenerator(seed: 4));
            List<string> outside = [];
            Dictionary<string, HashSet<Rarity>> seen = [];

            foreach (AbilityAugmentData record in ShippedAbilityData.Augments().All)
                for (int draw = 0; draw < 30; draw++)
                {
                    Rarity rolled = minter.Mint(record).Rarity;
                    (Rarity worst, Rarity best) = record.RarityBand;
                    if ((int)rolled < (int)best || (int)rolled > (int)worst)
                        outside.Add($"{record.Id}: {rolled} outside {worst}..{best}");
                    if (!seen.TryGetValue(record.Id, out HashSet<Rarity>? rarities)) seen[record.Id] = rarities = [];
                    rarities.Add(rolled);
                }

            Assert.AreEqual(0, outside.Count, $"copies minted outside their record's band: {string.Join(", ", outside.Distinct().Take(5))}");

            var wideBands = ShippedAbilityData.Augments().All
                .Where(record => record.RarityBand.Worst != record.RarityBand.Best)
                .Select(record => record.Id)
                .ToList();
            Assert.IsTrue(wideBands.Count > 0, "no shipped record declares a range, so the draw has nothing to prove");
            Assert.IsTrue(wideBands.All(id => seen[id].Count > 1),
                "a record declaring a range answered with one rarity in thirty draws: the band is not being drawn from");
        }

        [TestMethod]
        public void ACopyReadsTheSameEveryTimeAfterItIsMinted()
        {
            AugmentInstance copy = MinterOver(new DefaultRandomNumberGenerator(seed: 5)).Mint(ShippedRecord(ShareAugment));
            float first = copy.Values[ShareProperty];

            for (int read = 0; read < 100; read++)
                Assert.AreEqual(first, copy.Values[ShareProperty], "the copy's number moved between two reads of it");
        }

        [TestMethod]
        public void NoNumberOfNoShippedRecordEverComesOutAsNothing()
        {
            // Walked over everything the game ships, at every rarity its band reaches. A count is the
            // number a rounding can take away entirely, and an augment worth no turns is chosen, worn,
            // paid for and inert. Read off the rungs rather than sampled: without a draw the rungs ARE
            // the whole table of what a copy can be worth.
            AugmentMinter minter = MinterOver(new DefaultRandomNumberGenerator(seed: 4242));
            int walked = 0;

            foreach (AbilityAugmentData record in ShippedAbilityData.Augments().All)
                foreach ((string property, float declared) in record.UpgradeProperties)
                {
                    for (int step = (int)record.RarityBand.Worst; step >= (int)record.RarityBand.Best; step--)
                    {
                        float minted = minter.Mint(record, (Rarity)step).Values[property];
                        string what = $"'{record.Id}' is worth {minted} for '{property}' at {(Rarity)step} (declared {declared})";

                        Assert.AreNotEqual(0f, minted, $"{what} — nothing at all");
                        Assert.AreEqual(MathF.Sign(declared), MathF.Sign(minted), $"{what} — the other side of zero");
                        if (MathF.Round(declared) != declared) continue;

                        Assert.AreEqual(MathF.Round(minted), minted, $"{what} — a count as a fraction");
                        Assert.AreNotEqual(0, (int)minted, $"{what} — a count its consumer reads as none");
                    }

                    walked++;
                }

            Assert.IsTrue(walked > 0, "the shipped records carry no numbers at all, so the walk proves nothing");
        }

        [TestMethod]
        public void ACountIsWholeAtEveryRungAndAShareIsNot()
        {
            // A number written as a whole one is a count, and whoever consumes it takes the whole part
            // anyway: left fractional it would be truncated and the description would go on advertising
            // the fraction the augment never applies.
            bool fractionSeen = false;

            foreach (AbilityAugmentData record in ShippedAbilityData.Augments().All)
                foreach ((string property, float declared) in record.UpgradeProperties)
                {
                    (float atWorst, float atBest) = record.Ends(property);
                    for (int step = (int)record.RarityBand.Worst; step >= (int)record.RarityBand.Best; step--)
                    {
                        float rung = AugmentValueLadder.Rung(atWorst, atBest, record.RarityBand, (Rarity)step);
                        if (MathF.Round(declared) == declared)
                            Assert.AreEqual(MathF.Round(rung), rung, $"'{record.Id}.{property}' is a count and its rung is a fraction: {rung}");
                        else fractionSeen |= MathF.Abs(rung - MathF.Round(rung)) > 0f;
                    }
                }

            Assert.IsTrue(fractionSeen, "every rung came out whole, so the counts above prove nothing about counts");
        }

        [TestMethod]
        public void ACopyOfARecordNobodyDeclaresIsNotMinted()
        {
            Assert.IsNull(MinterOver(new DefaultRandomNumberGenerator(seed: 1)).Mint("Augment_No_File_Declares"));
        }

        [TestMethod]
        public void ARecordCarriesTheCopysNumbersAndKeepsWhatTheCopyNeverCarried()
        {
            // How a copy reaches everything built out of a record: the declaration unchanged, the
            // numbers replaced. A property added to the record after the copy was minted keeps its
            // declared base — the copy has nothing to say about a number that did not exist yet.
            AbilityAugmentData record = ShippedRecord(ShareAugment) with
            {
                UpgradeProperties = new Dictionary<string, float> { [ShareProperty] = 0.3f, ["addedLater"] = 7f }
            };
            var copy = new AugmentInstance(record.Id, new Dictionary<string, float> { [ShareProperty] = 0.375f }, Rarity.Common);

            AbilityAugmentData applied = copy.Applied(record);

            Assert.AreEqual(0.375f, applied.UpgradeProperties[ShareProperty], "the record kept its base instead of the copy's number");
            Assert.AreEqual(7f, applied.UpgradeProperties["addedLater"], "a property the copy never carried came out as nothing");
            Assert.AreEqual(0.3f, record.UpgradeProperties[ShareProperty], "the copy wrote its numbers into the record every copy shares");
        }

        [TestMethod]
        public void TheDescriptionPrintsTheNumberTheCopyCarriesAndNotTheRecordsBase()
        {
            // The tooltip is where a copy is read. Printing the record would tell the player what the
            // augment is worth at its PLAINEST while he wears a legendary one — the record declares the
            // worst rung — and description and behaviour are built from the same dictionary, so this is
            // also what keeps the two from drifting apart.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            AbilityAugmentData record = ShippedRecord(ShareAugment);
            AugmentInstance copy = new AugmentMinter(catalog, new DefaultRandomNumberGenerator(seed: 77))
                .Mint(record, Rarity.Legendary);

            IAugment? upgrade = registry.CreateUpgrade(copy);

            Assert.IsNotNull(upgrade, $"the registry builds nothing for a copy of '{ShareAugment}'");
            Assert.AreNotEqual(record.UpgradeProperties[ShareProperty], copy.Values[ShareProperty],
                "the copy came out at the record's own figure, so printing the base would pass either way");
            Assert.AreEqual(copy.Values[ShareProperty], upgrade.DescriptionValues[ShareProperty],
                "the description prints the record's base while the player wears a copy of it");
        }

        /// <summary>The minter the game composes, over the shipped records.</summary>
        private static AugmentMinter MinterOver(IRandomNumberGenerator rnd) => new(ShippedAbilityData.Augments(), rnd);

        /// <summary>One shipped record, read through the loader the game uses.</summary>
        private static AbilityAugmentData ShippedRecord(string augmentId)
        {
            AbilityAugmentData? record = ShippedAbilityData.Augments().Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            Assert.IsTrue(record.UpgradeProperties.Count > 0, $"'{augmentId}' carries no numbers");
            return record;
        }

        /// <summary>A generator that counts what is taken out of it.</summary>
        private sealed class CountingRandom(IRandomNumberGenerator inner) : IRandomNumberGenerator
        {
            public int Draws { get; private set; }

            public float RandFloat() => Counted(inner.RandFloat());

            public float RandFloatRange(float min, float max) => Counted(inner.RandFloatRange(min, max));

            public int RandIntRange(int min, int max) => Counted(inner.RandIntRange(min, max));

            public float RandFloatN(float mean, float deviation) => Counted(inner.RandFloatN(mean, deviation));

            public uint RandInt() => Counted(inner.RandInt());

            public long RandWeighted(float[] weights) => Counted(inner.RandWeighted(weights));

            public long RandWeighted(ReadOnlySpan<float> weights) => Counted(inner.RandWeighted(weights));

            public void Randomize() => inner.Randomize();

            private T Counted<T>(T value)
            {
                Draws++;
                return value;
            }
        }
    }
}
