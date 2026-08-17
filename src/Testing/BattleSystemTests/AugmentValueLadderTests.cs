namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity.Components;
    using Core.Enums;

    /// <summary>
    /// A design line that names a range names a RUNG PER RARITY, not a band to draw from. The rungs are
    /// written out below rather than recomputed, because the arithmetic is not the claim — the claim is
    /// that the owner's figures come out of it. Interpolate and round differently and every one of these
    /// still passes something; they only pass THESE numbers one way.
    /// </summary>
    [TestClass]
    public class AugmentValueLadderTests
    {
        /// <summary>The band most of the catalog rolls in, worst end first.</summary>
        private static readonly (Rarity Worst, Rarity Best) s_fourRungs = (Rarity.Uncommon, Rarity.Legendary);

        private static readonly Rarity[] s_fourRungOrder = [Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary];

        [TestMethod]
        public void TheOwnersOwnExampleComesOutAsTheOwnerWroteIt()
        {
            // "10–45% at Uncommon–Legendary" → 10 / 20 / 35 / 45. The two inner rungs interpolate to
            // 21.67 and 33.33 and land on fives; the ends are the author's and are not touched.
            float[] expected = [0.10f, 0.20f, 0.35f, 0.45f];

            for (int rung = 0; rung < s_fourRungOrder.Length; rung++)
                Assert.AreEqual(expected[rung], AugmentValueLadder.Rung(0.10f, 0.45f, s_fourRungs, s_fourRungOrder[rung]), 0.0001f,
                    $"the rung for {s_fourRungOrder[rung]} is not what the design list says");
        }

        [TestMethod]
        public void TheMulticastLadderComesOutOnFives()
        {
            // The owner's second named ladder: 25–90%. Interpolates to 46.67 and 68.33.
            float[] expected = [0.25f, 0.45f, 0.70f, 0.90f];

            for (int rung = 0; rung < s_fourRungOrder.Length; rung++)
                Assert.AreEqual(expected[rung], AugmentValueLadder.Rung(0.25f, 0.90f, s_fourRungs, s_fourRungOrder[rung]), 0.0001f,
                    $"the multicast rung for {s_fourRungOrder[rung]} moved");
        }

        [TestMethod]
        public void TheEndsAreTheAuthorsAndAreNeverRounded()
        {
            // A range whose ends are nowhere near a five still ends exactly where it was written: the
            // rounding is there to make the INVENTED rungs read as design, not to correct the author.
            Assert.AreEqual(0.07f, AugmentValueLadder.Rung(0.07f, 0.43f, s_fourRungs, Rarity.Uncommon), 0.0001f,
                "the worst rung was rounded away from what the record says");
            Assert.AreEqual(0.43f, AugmentValueLadder.Rung(0.07f, 0.43f, s_fourRungs, Rarity.Legendary), 0.0001f,
                "the best rung was rounded away from what the record says");
        }

        [TestMethod]
        public void NoRungFallsOutsideTheRangeItWasBuiltFrom()
        {
            foreach (Rarity rarity in s_fourRungOrder)
            {
                float rung = AugmentValueLadder.Rung(0.10f, 0.45f, s_fourRungs, rarity);
                Assert.IsTrue(rung is >= 0.10f and <= 0.45f, $"the {rarity} rung left the range: {rung}");
            }
        }

        [TestMethod]
        public void ARarerCopyIsNeverWorthLessThanAPlainerOne()
        {
            float previous = float.NegativeInfinity;
            foreach (Rarity rarity in s_fourRungOrder)
            {
                float rung = AugmentValueLadder.Rung(0.10f, 0.45f, s_fourRungs, rarity);
                Assert.IsTrue(rung >= previous, $"the {rarity} rung is below the one under it — the ladder runs backwards");
                previous = rung;
            }
        }

        [TestMethod]
        public void ACountLaddersInWholeStepsAndNotInFives()
        {
            // Rounding a 1–3 ladder to fives would collapse it into one rung, so counts step by one.
            float[] expected = [1f, 2f, 2f, 3f];

            for (int rung = 0; rung < s_fourRungOrder.Length; rung++)
                Assert.AreEqual(expected[rung], AugmentValueLadder.Rung(1f, 3f, s_fourRungs, s_fourRungOrder[rung]), 0.0001f,
                    $"the count rung for {s_fourRungOrder[rung]} is not a whole step");
        }

        [TestMethod]
        public void ARecordThatNamesOneNumberIsWorthItAtEveryRarity()
        {
            foreach (Rarity rarity in s_fourRungOrder)
                Assert.AreEqual(0.15f, AugmentValueLadder.Rung(0.15f, 0.15f, s_fourRungs, rarity), 0.0001f,
                    $"a record with no range came out different at {rarity}");

            Assert.IsFalse(AugmentValueLadder.IsLadder(0.15f, 0.15f), "one number was taken for a ladder");
            Assert.IsTrue(AugmentValueLadder.IsLadder(0.10f, 0.45f), "a range was taken for a constant");
        }

        [TestMethod]
        public void ARarityOutsideTheBandGetsTheNearestRungTheRecordActuallyDescribes()
        {
            // A loot table may seat a copy at a rarity the record never rolls. What it gets is an end,
            // never an extrapolation past one.
            Assert.AreEqual(0.10f, AugmentValueLadder.Rung(0.10f, 0.45f, s_fourRungs, Rarity.Common), 0.0001f,
                "a rarity below the band was extrapolated instead of clamped");
            // Mythic and Unique sit OFF this scale by number (10 and 11 against Legendary's zero) — they
            // are template rarities generation never rolls, so the ladder has nothing to say about them
            // and they fall to the plain end like anything else below the band.
            Assert.AreEqual(0.10f, AugmentValueLadder.Rung(0.10f, 0.45f, s_fourRungs, Rarity.Mythic), 0.0001f,
                "a template rarity was extrapolated instead of falling to an end the record wrote");
        }

        [TestMethod]
        public void AMintedCopyIsWorthItsOwnRarityRungAndNotTheRecordsFirstNumber()
        {
            // The whole of CL-5 in one measurement. Before the ladder every copy of a record was worth ONE
            // figure whatever it rolled on the rarity scale; now the rarity decides the figure exactly.
            var record = new AbilityAugmentData
            {
                Id = "Augment_Ladder_Probe",
                MinRarity = Rarity.Uncommon,
                MaxRarity = Rarity.Legendary,
                UpgradeProperties = new Dictionary<string, float> { ["value"] = 0.10f },
                BestRarityProperties = new Dictionary<string, float> { ["value"] = 0.45f }
            };

            var minter = new AugmentMinter(CatalogOf(record), new DefaultRandomNumberGenerator(seed: 20260814));

            foreach (Rarity rarity in s_fourRungOrder)
            {
                float rung = AugmentValueLadder.Rung(0.10f, 0.45f, record.RarityBand, rarity);

                Assert.AreEqual(rung, minter.Mint(record, rarity).Values["value"], 0.0001f,
                    $"a {rarity} copy is not worth its own rung {rung}");
            }
        }

        [TestMethod]
        public void ACopyRestoredFromASaveKeepsTheNumbersItWasMintedWithAndIsNotRunUpTheLadder()
        {
            // The ladder is a MINT-time rule. A copy already in the world was drawn under whatever the
            // record said then, and re-deriving it on load would silently rebalance everything the
            // player already owns — the one thing a value kept "for good" must never do.
            var saved = new Dictionary<string, float> { ["value"] = 0.123f };
            var restored = new AugmentInstance("Augment_Ladder_Probe", saved, Rarity.Legendary);

            Assert.AreEqual(0.123f, restored.Values["value"], 0.0001f,
                "a restored copy no longer carries the number it was saved with");
        }

        [TestMethod]
        public void NoShippedRecordDeclaresALadderItsRarityBandCannotClimb()
        {
            // A band of one rarity has one rung, and the ladder answers it with the BEST end. So a
            // Legendary-only record naming two different numbers has a declared value nobody ever mints
            // and a minted value nobody declared — and the two doors disagree, because an un-minted
            // upgrade is built from the declaration while a copy comes from the rung. Six records
            // shipped in exactly that state and seven of their numbers were quietly minting between
            // 2.3 and 5 times what the record said.
            List<string> broken = [];

            foreach (AbilityAugmentData record in ShippedAbilityData.Augments().All)
            {
                (Rarity worst, Rarity best) = record.RarityBand;
                if (worst != best) continue;

                foreach (string property in record.UpgradeProperties.Keys)
                {
                    (float atWorst, float atBest) = record.Ends(property);
                    if (AugmentValueLadder.IsLadder(atWorst, atBest))
                        broken.Add($"{record.Id}.{property}: declares {atWorst} and mints {atBest} at its only rarity ({worst})");
                }
            }

            Assert.AreEqual(0, broken.Count,
                "records whose band holds one rarity and whose ends disagree — what they declare is not what they mint:\n  "
                + string.Join("\n  ", broken));
        }

        [TestMethod]
        public void EveryShippedLadderRunsTheRightWayAndStaysInsideItsOwnEnds()
        {
            List<string> broken = [];

            foreach (AbilityAugmentData record in ShippedAbilityData.Augments().All)
                foreach (string property in record.UpgradeProperties.Keys)
                {
                    (float atWorst, float atBest) = record.Ends(property);
                    if (!AugmentValueLadder.IsLadder(atWorst, atBest)) continue;

                    float low = MathF.Min(atWorst, atBest);
                    float high = MathF.Max(atWorst, atBest);
                    float previous = float.NegativeInfinity;

                    for (int step = (int)record.RarityBand.Worst; step >= (int)record.RarityBand.Best; step--)
                    {
                        float rung = AugmentValueLadder.Rung(atWorst, atBest, record.RarityBand, (Rarity)step);
                        if (rung < low || rung > high) broken.Add($"{record.Id}.{property}: rung {rung} at {(Rarity)step} is outside [{low}..{high}]");
                        if (rung < previous) broken.Add($"{record.Id}.{property}: the ladder falls at {(Rarity)step}");
                        previous = rung;
                    }
                }

            Assert.AreEqual(0, broken.Count, $"shipped ladders that do not hold:\n  {string.Join("\n  ", broken)}");
        }

        /// <summary>The rungs the owner wrote out by hand, worst rarity first. Transcribed, because the
        /// point of an authored ladder is that it is NOT derivable — reading it off the file it is
        /// checked against would leave nothing checked.</summary>
        private static readonly (string Augment, string Property, float[] Rungs)[] s_authored =
        [
            ("Augment_Additional_Health_Regen", "additionalRegen", [0.01f, 0.02f, 0.035f, 0.05f]),
            ("Augment_Fury_More_Burn", "amount", [0.01f, 0.02f, 0.035f, 0.055f]),
            ("Augment_Fury_Less_Burn", "amount", [0.01f, 0.015f, 0.025f, 0.035f]),
            ("Augment_Health_Bonus", "healthBonus", [0.05f, 0.075f, 0.10f, 0.15f]),
            ("Augment_Cooldown_Chance", "chance", [0.05f, 0.075f, 0.10f, 0.15f]),
            ("Augment_Heal_On_Hit", "amount", [0.03f, 0.05f, 0.07f, 0.10f]),
            ("Augment_Additional_Projectiles", "amount", [1f, 2f, 3f, 4f]),
        ];

        [TestMethod]
        public void TheAuthoredLaddersMintExactlyTheRungsTheOwnerWroteOut()
        {
            AugmentMinter minter = new(ShippedAbilityData.Augments(), new DefaultRandomNumberGenerator(seed: 1));
            List<string> wrong = [];

            foreach ((string augmentId, string property, float[] rungs) in s_authored)
            {
                AbilityAugmentData? record = ShippedAbilityData.Augments().Find(augmentId);
                Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
                Assert.IsNotNull(record.AuthoredRungs(property), $"'{augmentId}' no longer writes its rungs out by hand");

                for (int rung = 0; rung < rungs.Length; rung++)
                {
                    var rarity = (Rarity)((int)record.RarityBand.Worst - rung);
                    float minted = minter.Mint(record, rarity).Values[property];
                    if (Math.Abs(minted - rungs[rung]) > 0.0001f)
                        wrong.Add($"{augmentId}.{property} at {rarity}: expected {rungs[rung]}, minted {minted}");
                }
            }

            Assert.AreEqual(0, wrong.Count, $"authored rungs the catalog no longer mints:\n  {string.Join("\n  ", wrong)}");
        }

        [TestMethod]
        public void EveryAuthoredLadderHasARungPerRarityAndClimbs()
        {
            List<string> broken = [];

            foreach (AbilityAugmentData record in ShippedAbilityData.Augments().All)
                foreach ((string property, float[] rungs) in record.RarityLadder)
                {
                    int expected = AugmentValueLadder.RungCount(record.RarityBand);
                    if (rungs.Length != expected)
                        broken.Add($"{record.Id}.{property}: {rungs.Length} rungs for a band of {expected} rarities");

                    if (!record.UpgradeProperties.TryGetValue(property, out float declared))
                        broken.Add($"{record.Id}.{property}: a ladder for a property the record does not declare");
                    else if (rungs.Length > 0 && Math.Abs(rungs[0] - declared) > 0.0001f)
                        broken.Add($"{record.Id}.{property}: declares {declared} and its worst rung is {rungs[0]}");

                    for (int rung = 1; rung < rungs.Length; rung++)
                        if (MathF.Abs(rungs[rung]) < MathF.Abs(rungs[rung - 1]))
                            broken.Add($"{record.Id}.{property}: rung {rung} ({rungs[rung]}) is below the one under it");
                }

            Assert.AreEqual(0, broken.Count, $"authored ladders that do not hold:\n  {string.Join("\n  ", broken)}");
        }

        private static IAbilityAugmentCatalog CatalogOf(AbilityAugmentData record)
        {
            var catalog = new Moq.Mock<IAbilityAugmentCatalog>();
            catalog.Setup(source => source.Find(record.Id)).Returns(record);
            return catalog.Object;
        }
    }
}
