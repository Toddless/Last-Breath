namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Battle.Source.CombatRules;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Entity.Components;

    /// <summary>
    /// An augment is a copy of its record, not the record itself. The record says what the augment is
    /// about and what its numbers are worth on average; a copy is one draw around those numbers, taken
    /// once when the copy is minted and kept for good — so two augments of one id are no longer the
    /// same augment, and an id is no longer enough to say which one is meant.
    ///
    /// Two things the draw has to answer for, and both are walked here. It has to stay inside the band
    /// the rules declare, or the record's number stops meaning anything. And it must never land on
    /// nothing: a fifth of the shipped numbers are counts — turns, attacks, stacks — a quarter off one
    /// of them is three quarters of a turn, and an augment worth zero turns is chosen, worn, paid for
    /// and silently does nothing at all.
    /// </summary>
    [TestClass]
    public class AugmentInstanceTests
    {
        /// <summary>The band the shipped rules open. Written out rather than read off the provider:
        /// this is the second copy the claim needs, and a file edited to zero would otherwise make
        /// every walk below agree with it.</summary>
        private const float ShippedSpread = 0.25f;

        /// <summary>Draws per number where a walk is about the shape of the distribution rather than
        /// about one value. High enough for the extremes of the band to be approached.</summary>
        private const int Draws = 2000;

        /// <summary>A record of the shipped catalog whose numbers are shares, so a draw of one is a
        /// value of its own rather than a rounded count.</summary>
        private const string ShareAugment = "Augment_Reduce_Cost";

        private const string ShareProperty = "costShare";

        /// <summary>Widths the band is walked at where a claim is about the shape of the rule rather
        /// than about the figure the file happens to carry: the shipped one, one far wider, and one
        /// wide enough to take the whole of a base away. A guarantee held only at the width shipped
        /// today is a guarantee the next balance pass silently repeals.</summary>
        private static readonly float[] s_widths = [ShippedSpread, 0.95f, 4f];

        [TestMethod]
        public void TheSpreadComesFromTheRulesFileAndNotFromTheCode()
        {
            // The number lives in the configuration catalog the rest of the combat constants live in,
            // so a balance pass moves one figure instead of a hundred and thirty-four records. Read
            // through the loader the game runs: a section nothing parses is a spread of zero, which is
            // every augment pinned to its base and no walk below able to tell.
            AugmentValueRules rules = ShippedRules().AugmentValues;

            Assert.AreEqual(ShippedSpread, rules.ValueSpread, "the shipped rules no longer open the band the design asks for");
            Assert.IsTrue(rules.Rolls);
        }

        [TestMethod]
        public void ADrawStaysInsideTheBandAndUsesTheWholeOfIt()
        {
            // Both halves of a spread: it bounds the draw, and the draw is actually spread. A value
            // that never left the base would pass a bounds check on its own.
            AbilityAugmentData record = ShippedRecord(ShareAugment);
            float declared = record.UpgradeProperties[ShareProperty];
            AugmentMinter minter = MinterOver(ShippedSpread, new DefaultRandomNumberGenerator(seed: 20260803));
            float low = float.MaxValue;
            float high = float.MinValue;

            for (int draw = 0; draw < Draws; draw++)
            {
                float rolled = minter.Mint(record).Values[ShareProperty];
                low = MathF.Min(low, rolled);
                high = MathF.Max(high, rolled);
            }

            Assert.IsTrue(low >= declared * (1f - ShippedSpread), $"a draw fell below the band: {low}");
            Assert.IsTrue(high <= declared * (1f + ShippedSpread), $"a draw rose above the band: {high}");
            Assert.IsTrue(low < declared * 0.78f, $"nothing was drawn near the bottom of the band: {low}");
            Assert.IsTrue(high > declared * 1.22f, $"nothing was drawn near the top of the band: {high}");
        }

        [TestMethod]
        public void TwoCopiesOfOneRecordAreNotTheSameAugment()
        {
            // The consequence the whole change exists for: one player's copy is worth more than
            // another's, so nothing that cares WHICH copy may address it by the record's id.
            AbilityAugmentData record = ShippedRecord(ShareAugment);
            AugmentMinter minter = MinterOver(ShippedSpread, new DefaultRandomNumberGenerator(seed: 7));

            AugmentInstance first = minter.Mint(record);
            AugmentInstance second = minter.Mint(record);

            Assert.AreEqual(first.AugmentId, second.AugmentId, "the two copies are not copies of one record at all");
            Assert.AreNotEqual(first.Values[ShareProperty], second.Values[ShareProperty],
                "two draws produced the same number, so the copies are the record after all");
        }

        [TestMethod]
        public void TheSameDrawMintsTheSameCopy()
        {
            // The other side of it: the difference is the draw and nothing else. A copy that varied
            // with anything but the generator could not be reproduced, and a seeded run would stop
            // being a run of the same game.
            AbilityAugmentData record = ShippedRecord(ShareAugment);

            AugmentInstance first = MinterOver(ShippedSpread, new DefaultRandomNumberGenerator(seed: 99)).Mint(record);
            AugmentInstance second = MinterOver(ShippedSpread, new DefaultRandomNumberGenerator(seed: 99)).Mint(record);

            Assert.AreEqual(first.Values[ShareProperty], second.Values[ShareProperty]);
        }

        [TestMethod]
        public void EveryCopyLandsInsideItsRecordsBandAndAWideBandDoesNotAlwaysAnswerTheSame()
        {
            // The rarity is a draw like the numbers are, taken at the same seam and kept for good. Two
            // ways for it to go wrong without anything failing: a draw that leaves the band the record
            // declares — the copy is then worth something its own author never allowed — and a "draw"
            // that always answers the same, which is an authored rarity wearing a band's clothes.
            AugmentMinter minter = MinterOver(ShippedSpread, new DefaultRandomNumberGenerator(seed: 4));
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
        public void ACopyIsDrawnOnceAndReadsTheSameEveryTimeAfter()
        {
            // A value recomputed on every read would move between two looks at the same tooltip, and
            // between the tooltip and the ability wearing it.
            AugmentInstance copy = MinterOver(ShippedSpread, new DefaultRandomNumberGenerator(seed: 5))
                .Mint(ShippedRecord(ShareAugment));

            float first = copy.Values[ShareProperty];

            for (int read = 0; read < 100; read++)
                Assert.AreEqual(first, copy.Values[ShareProperty], "the copy's number moved between two reads of it");
        }

        [TestMethod]
        public void ACountStaysWholeAndAShareDoesNot()
        {
            // A number written as a whole one is a count — turns, attacks, stacks — and whoever
            // consumes it takes the whole part anyway: left fractional it would be truncated, and the
            // description would go on advertising the fraction the augment never applies.
            AugmentMinter minter = MinterOver(ShippedSpread, new DefaultRandomNumberGenerator(seed: 11));
            AbilityAugmentData counts = Record("Augment_Test_Counts", ("duration", 5f));
            AbilityAugmentData shares = Record("Augment_Test_Shares", ("chance", 0.5f));
            bool fractionSeen = false;

            for (int draw = 0; draw < Draws; draw++)
            {
                float count = minter.Mint(counts).Values["duration"];
                Assert.AreEqual(MathF.Round(count), count, $"a count was drawn as a fraction: {count}");

                float share = minter.Mint(shares).Values["chance"];
                fractionSeen |= MathF.Abs(share - MathF.Round(share)) > 0f;
            }

            Assert.IsTrue(fractionSeen, "a share is being rounded too, so the counts above prove nothing about counts");
        }

        [TestMethod]
        public void NoNumberOfNoShippedRecordEverComesOutAsNothing()
        {
            // The failure the rounding exists to rule out, walked over everything the game ships: a
            // quarter off a single turn is three quarters of one, and whoever consumes a count takes
            // its whole part — three quarters of a turn is no turns at all, and an augment worth no
            // turns is chosen, worn, paid for and inert.
            WalkEveryShippedNumber(ShippedSpread, seed: 4242);
        }

        [TestMethod]
        public void AWiderBandStillNeverDrawsACountDownToNothing()
        {
            // The same walk with the band opened far enough for a count of one to be drawn under a
            // half. The guarantee is not a property of the shipped spread — the spread is a balance
            // figure and moves — so it is held where a quarter cannot reach: the floor under a count
            // is what keeps the augment from silently doing nothing at any width.
            WalkEveryShippedNumber(spread: 0.95f, seed: 606);
        }

        [TestMethod]
        public void EveryCountTheGameShipsLandsBetweenTwoBoundsAndNeitherOfThemIsNothing()
        {
            // The same claim as the two walks above, read off the band instead of sampled out of it.
            // A draw lands where it lands through one rounding, and rounding is monotone — a higher
            // draw never comes out lower — so the two edges of the band bound everything between them,
            // and putting every count of every shipped record through both edges is the whole table of
            // what a count can be worth rather than two thousand attempts at finding a hole in it.
            IReadOnlyList<(AbilityAugmentData Record, string Property, float Declared)> counts = ShippedCounts();
            Assert.IsTrue(counts.Count > 0, "the shipped records carry no counts, so the bounds below are nobody's");

            foreach (float spread in s_widths)
            {
                AugmentMinter bottom = MinterOver(spread, new EdgeRandom(atBottom: true));
                AugmentMinter top = MinterOver(spread, new EdgeRandom(atBottom: false));

                foreach ((AbilityAugmentData record, string property, float declared) in counts)
                {
                    float atBottom = bottom.Mint(record).Values[property];
                    float atTop = top.Mint(record).Values[property];
                    string bounds = $"'{record.Id}' draws '{property}' (base {declared}) between {atBottom} and {atTop} at a spread of {spread}";

                    Assert.AreEqual(MathF.Sign(declared), MathF.Sign(atBottom), $"{bounds} — the bottom is on the other side of nothing");
                    Assert.IsTrue(MathF.Abs(atBottom) >= 1f, $"{bounds} — the bottom of the band is a count of none");
                    Assert.IsTrue(MathF.Abs(atTop) >= MathF.Abs(atBottom), $"{bounds} — the band has no inside");
                    Assert.AreEqual(MathF.Round(declared * (1f + spread), MidpointRounding.AwayFromZero), atTop,
                        $"{bounds} — the top is not the one the spread declares");
                }
            }
        }

        [TestMethod]
        public void ABandOfNoWidthPinsEveryCopyToItsRecordAndDrawsNothing()
        {
            // What a composition holding no rules file gets, and the control the band needs: with the
            // spread closed the copy IS the record. The generator is left untouched as well — a draw
            // spent on a band of no width would shift every seeded sequence taken after it. The rarity
            // is handed in rather than drawn, so the only draw left to count is the numbers'.
            var rnd = new CountingRandom(new DefaultRandomNumberGenerator(seed: 3));
            AbilityAugmentData record = ShippedRecord(ShareAugment);

            AugmentInstance copy = new AugmentMinter(ShippedAbilityData.Augments(), RulesOf(AugmentValueRules.Fixed), rnd)
                .Mint(record, record.RarityBand.Worst);

            Assert.AreEqual(record.UpgradeProperties[ShareProperty], copy.Values[ShareProperty]);
            Assert.AreEqual(0, rnd.Draws, "a band of no width still spent a draw");
        }

        [TestMethod]
        public void ACopyOfARecordNobodyDeclaresIsNotMinted()
        {
            // Nothing says what to draw around. A copy carrying no numbers would be an augment that
            // parses, seats and does nothing.
            AugmentMinter minter = MinterOver(ShippedSpread, new DefaultRandomNumberGenerator(seed: 1));

            Assert.IsNull(minter.Mint("Augment_No_File_Declares"));
        }

        [TestMethod]
        public void ARecordCarriesTheCopysNumbersAndKeepsWhatTheCopyNeverRolled()
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
            Assert.AreEqual(7f, applied.UpgradeProperties["addedLater"], "a property the copy never rolled came out as nothing");
            Assert.AreEqual(0.3f, record.UpgradeProperties[ShareProperty], "the copy wrote its numbers into the record every copy shares");
        }

        [TestMethod]
        public void TheDescriptionPrintsTheNumberTheCopyRolledAndNotTheRecordsBase()
        {
            // The tooltip is where a copy is read. Printing the record would tell the player what the
            // augment is worth on average while he wears one that is worth something else — and the
            // description and the behaviour are built from the same dictionary, so this is also what
            // keeps the two from drifting apart.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            AbilityAugmentData record = ShippedRecord(ShareAugment);
            AugmentInstance copy = new AugmentMinter(catalog, RulesOf(new AugmentValueRules(ShippedSpread)),
                new DefaultRandomNumberGenerator(seed: 77)).Mint(record);

            IAbilityAugment? upgrade = registry.CreateUpgrade(copy);

            Assert.IsNotNull(upgrade, $"the registry builds nothing for a copy of '{ShareAugment}'");
            Assert.AreNotEqual(record.UpgradeProperties[ShareProperty], copy.Values[ShareProperty],
                "the copy came out at its base, so printing the base would pass either way");
            Assert.AreEqual(copy.Values[ShareProperty], upgrade.DescriptionValues[ShareProperty],
                "the description prints the record's base while the player wears a copy of it");
        }

        /// <summary>
        /// Every number of every shipped record, drawn again and again at that width. Three things are
        /// held on each draw: it is not nothing, it did not cross to the other side of zero — an
        /// augment doing the opposite of what it says — and a count came out as a count, which is what
        /// makes the number the description prints survive the truncation its consumer performs.
        /// </summary>
        private static void WalkEveryShippedNumber(float spread, int seed)
        {
            AugmentMinter minter = MinterOver(spread, new DefaultRandomNumberGenerator(seed));
            int walked = 0;

            foreach (AbilityAugmentData record in ShippedAbilityData.Augments().All)
                foreach ((string property, float declared) in record.UpgradeProperties)
                {
                    for (int draw = 0; draw < 200; draw++)
                    {
                        float rolled = minter.Mint(record).Values[property];
                        string drawn = $"'{record.Id}' drew {rolled} for '{property}' (base {declared})";

                        Assert.AreNotEqual(0f, rolled, $"{drawn} — nothing at all");
                        Assert.AreEqual(MathF.Sign(declared), MathF.Sign(rolled), $"{drawn} — the other side of zero");
                        if (MathF.Round(declared) != declared) continue;

                        Assert.AreEqual(MathF.Round(rolled), rolled, $"{drawn} — a count as a fraction");
                        Assert.AreNotEqual(0, (int)rolled, $"{drawn} — a count its consumer reads as none");
                    }

                    walked++;
                }

            Assert.IsTrue(walked > 0, "the shipped records carry no numbers at all, so the walk proves nothing");
        }

        /// <summary>Every whole number of every shipped record: the counts, which are the numbers a
        /// rounding can take away entirely.</summary>
        private static IReadOnlyList<(AbilityAugmentData Record, string Property, float Declared)> ShippedCounts() =>
        [
            .. ShippedAbilityData.Augments().All
                .SelectMany(record => record.UpgradeProperties
                    .Where(number => MathF.Round(number.Value) == number.Value)
                    .Select(number => (record, number.Key, number.Value)))
        ];

        /// <summary>The minter the game composes, over the shipped records and a band of that width.</summary>
        private static AugmentMinter MinterOver(float spread, IRandomNumberGenerator rnd) =>
            new(ShippedAbilityData.Augments(), RulesOf(new AugmentValueRules(spread)), rnd);

        private static ICombatRulesProvider RulesOf(AugmentValueRules values) => new StubCombatRules(values);

        /// <summary>One shipped record, read through the loader the game uses.</summary>
        private static AbilityAugmentData ShippedRecord(string augmentId)
        {
            AbilityAugmentData? record = ShippedAbilityData.Augments().Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            Assert.IsTrue(record.UpgradeProperties.Count > 0, $"'{augmentId}' carries no numbers to draw around");
            return record;
        }

        /// <summary>A record the shipped files do not declare, for the properties a walk needs.</summary>
        private static AbilityAugmentData Record(string id, params (string Property, float Value)[] properties) =>
            new()
            {
                Id = id,
                UpgradeProperties = properties.ToDictionary(entry => entry.Property, entry => entry.Value)
            };

        /// <summary>The combat rules as the shipped configuration catalog declares them, through the
        /// real loader.</summary>
        private static ICombatRulesProvider ShippedRules()
        {
            var provider = new CombatRulesProvider();
            var service = new GameDataService(new FileSystemDataSource(SharedData.Root()), [provider]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            return provider;
        }

        /// <summary>A generator pinned to one edge of every range it is asked for — what the extremes
        /// of the band actually produce, rather than what a seeded run gets near. It answers the range
        /// draws alone: a value reaching the copy through any other door of the generator would not be
        /// the one the bounds below are read from. The mint takes two of them — the value's range and
        /// the rarity's — and both are pinned the same way.</summary>
        private sealed class EdgeRandom(bool atBottom) : IRandomNumberGenerator
        {
            private const string OtherDoor = "the roll took a number out of the generator some other way than as a range";

            public float RandFloatRange(float min, float max) => atBottom ? min : max;

            public float RandFloat() => throw new NotSupportedException(OtherDoor);

            public int RandIntRange(int min, int max) => atBottom ? min : max;

            public float RandFloatN(float mean, float deviation) => throw new NotSupportedException(OtherDoor);

            public uint RandInt() => throw new NotSupportedException(OtherDoor);

            public long RandWeighted(float[] weights) => throw new NotSupportedException(OtherDoor);

            public long RandWeighted(ReadOnlySpan<float> weights) => throw new NotSupportedException(OtherDoor);

            public void Randomize() => throw new NotSupportedException(OtherDoor);
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
