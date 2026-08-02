namespace LastBreathTest.BattleSystemTests
{
    using System.Text.RegularExpressions;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Enums;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// An augment declares what it is — its tier, what it is about, the one ability it was written
    /// for — and a slot is measured against that declaration. A slot takes its own tier and every
    /// tier under it, a named ability is the whole answer where it is given and the tags answer
    /// where it is not, and one ability wears at most one augment of an exclusion group. What does
    /// not fit does not go in quietly: the install refuses and nothing moves.
    /// </summary>
    [TestClass]
    public class AugmentFitTests
    {
        private const string PoisonAbility = "Ability_Poison_Coating";
        private const string ColdAbility = "Ability_Ice_Shards";

        private const string PoisonSlotOne = "socket_poison_one";
        private const string PoisonSlotTwo = "socket_poison_two";
        private const string ColdSlot = "socket_cold";

        private const string LowAugment = "Augment_Poison_Tier_One";
        private const string EqualAugment = "Augment_Poison_Tier_Two";
        private const string HighAugment = "Augment_Poison_Tier_Three";
        private const string ColdAugment = "Augment_Cold_Tier_One";
        private const string BoundAugment = "Augment_Written_For_One_Ability";
        private const string GroupedAugment = "Augment_Poison_Duration";
        private const string SameGroupAugment = "Augment_Poison_Duration_Greater";
        private const string OtherGroupAugment = "Augment_Poison_Spread";
        private const string UnknownAugment = "Augment_No_Catalog_Holds";

        private const string DurationGroup = "Group_Poison_Duration";
        private const string SpreadGroup = "Group_Poison_Spread";

        /// <summary>A tier held against another tier: the shape of the tier rule wherever it is
        /// written, in members (<c>augment.Tier</c>) or in locals (<c>augmentTier</c>).</summary>
        private static readonly Regex s_tierComparison = new(@"Tier[^;]*\s(<=|>=|<|>)\s[^;]*Tier", RegexOptions.Compiled);

        /// <summary>Both ways one set of tags is held against another.</summary>
        private static readonly string[] s_tagComparisons = ["SharesAny", ".Intersect("];

        /// <summary>What makes a source file one the rule could be rewritten in: it speaks either
        /// about the slots or about the records the rule reads.</summary>
        private static readonly string[] s_subsystemMarkers = ["AbilitySocket", "AugmentFit", "AbilityUpgradeData"];

        private static readonly string[] s_generatedFolders = ["bin", "obj", ".godot", "Testing"];

        private static string SrcRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                    directory = directory.Parent;
                Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
                return directory.FullName;
            }
        }

        [TestMethod]
        public void ASlotTakesAnAugmentOfALowerTier()
        {
            // Low tiers never become dead weight: the tier-three slot of a long build still wears the
            // tier-one augment the character has been carrying since the first node.
            var board = BoardOver(
                PoisonCatalog().With(Augment(LowAugment, tier: 1, [AbilityTags.Poison])),
                Slot(PoisonSlotOne, PoisonAbility, tier: 3));

            Assert.IsTrue(board.Install(PoisonSlotOne, LowAugment));
            Assert.AreEqual(LowAugment, board.Find(PoisonSlotOne)?.Augment);
        }

        [TestMethod]
        public void ASlotTakesAnAugmentOfItsOwnTier()
        {
            var board = BoardOver(
                PoisonCatalog().With(Augment(EqualAugment, tier: 2, [AbilityTags.Poison])),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2));

            Assert.IsTrue(board.Install(PoisonSlotOne, EqualAugment));
            Assert.AreEqual(EqualAugment, board.Find(PoisonSlotOne)?.Augment);
        }

        [TestMethod]
        public void ASlotRefusesAnAugmentAboveItsTierAndKeepsItsHandsEmpty()
        {
            var board = BoardOver(
                PoisonCatalog().With(Augment(HighAugment, tier: 3, [AbilityTags.Poison])),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2));

            Assert.IsFalse(board.Install(PoisonSlotOne, HighAugment));

            Assert.IsTrue(board.Find(PoisonSlotOne)?.IsEmpty, "the augment went in against the rule");
            Assert.AreEqual(0, board.Occupants.Count, "the refused augment is still written into the save");
        }

        [TestMethod]
        public void AnUnboundAugmentGoesOnAnAbilitySharingOneOfItsTags()
        {
            var board = BoardOver(
                PoisonCatalog().With(Augment(EqualAugment, tier: 2, [AbilityTags.Poison, AbilityTags.Spell])),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2));

            Assert.IsTrue(board.Install(PoisonSlotOne, EqualAugment));
        }

        [TestMethod]
        public void AnUnboundAugmentSharingNoTagIsRefused()
        {
            var board = BoardOver(
                PoisonCatalog().With(Augment(ColdAugment, tier: 1, [AbilityTags.Cold])),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2));

            Assert.IsFalse(board.Install(PoisonSlotOne, ColdAugment));
            Assert.IsTrue(board.Find(PoisonSlotOne)?.IsEmpty);
        }

        [TestMethod]
        public void AnAugmentNamingItsAbilityGoesInWithoutSharingATag()
        {
            // The binding is the whole answer where it is given: an augment written for one ability
            // is not also asked to carry a tag of it.
            var board = BoardOver(
                PoisonCatalog().With(Augment(BoundAugment, tier: 1, [AbilityTags.Cold], abilityId: PoisonAbility)),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2));

            Assert.IsTrue(board.Install(PoisonSlotOne, BoundAugment));
            Assert.AreEqual(BoundAugment, board.Find(PoisonSlotOne)?.Augment);
        }

        [TestMethod]
        public void AnAugmentNamingAnotherAbilityIsRefusedThoughTheTagsMatch()
        {
            // The other half of the same rule: a shared tag cannot let a bound augment onto an
            // ability it was not written for, or the binding would be a suggestion.
            var board = BoardOver(
                PoisonCatalog().With(Augment(BoundAugment, tier: 1, [AbilityTags.Poison], abilityId: ColdAbility)),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2));

            Assert.IsFalse(board.Install(PoisonSlotOne, BoundAugment));
            Assert.IsTrue(board.Find(PoisonSlotOne)?.IsEmpty);
        }

        [TestMethod]
        public void OneAbilityWearsOneAugmentOfAnExclusionGroup()
        {
            var board = BoardOver(
                PoisonCatalog()
                    .With(Augment(GroupedAugment, tier: 2, [AbilityTags.Poison], exclusionGroup: DurationGroup))
                    .With(Augment(SameGroupAugment, tier: 2, [AbilityTags.Poison], exclusionGroup: DurationGroup)),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2),
                Slot(PoisonSlotTwo, PoisonAbility, tier: 2));
            Assert.IsTrue(board.Install(PoisonSlotOne, GroupedAugment), "the first of the group would not go in at all");

            Assert.IsFalse(board.Install(PoisonSlotTwo, SameGroupAugment));

            Assert.IsTrue(board.Find(PoisonSlotTwo)?.IsEmpty);
            Assert.AreEqual(GroupedAugment, board.Find(PoisonSlotOne)?.Augment, "the refusal took the seated one with it");
        }

        [TestMethod]
        public void AnAugmentOfAnotherGroupStillGoesOnTheSameAbility()
        {
            // The control the refusal above needs: the second slot is not simply closed once the
            // first one is filled.
            var board = BoardOver(
                PoisonCatalog()
                    .With(Augment(GroupedAugment, tier: 2, [AbilityTags.Poison], exclusionGroup: DurationGroup))
                    .With(Augment(OtherGroupAugment, tier: 2, [AbilityTags.Poison], exclusionGroup: SpreadGroup)),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2),
                Slot(PoisonSlotTwo, PoisonAbility, tier: 2));
            board.Install(PoisonSlotOne, GroupedAugment);

            Assert.IsTrue(board.Install(PoisonSlotTwo, OtherGroupAugment));
        }

        [TestMethod]
        public void AGroupWornByOneAbilityDoesNotBlockAnother()
        {
            // The group says one of these at a time on one ability. Two abilities each wearing the
            // duration augment of their own family is the point of the augment, not a conflict.
            var board = BoardOver(
                PoisonCatalog()
                    .WithAbility(ColdAbility, AbilityTags.Cold)
                    .With(Augment(GroupedAugment, tier: 2, [AbilityTags.Poison], exclusionGroup: DurationGroup))
                    .With(Augment(SameGroupAugment, tier: 2, [AbilityTags.Cold], exclusionGroup: DurationGroup)),
                Slot(PoisonSlotOne, PoisonAbility, tier: 2),
                Slot(ColdSlot, ColdAbility, tier: 2));
            board.Install(PoisonSlotOne, GroupedAugment);

            Assert.IsTrue(board.Install(ColdSlot, SameGroupAugment));
        }

        [TestMethod]
        public void AnAugmentTheCatalogDoesNotHoldIsRefused()
        {
            // Nothing declares this id, so nothing can say it belongs. Seating it on the strength of
            // its spelling is how an augment ends up in a slot nobody agreed to.
            var board = BoardOver(PoisonCatalog(), Slot(PoisonSlotOne, PoisonAbility, tier: 3));

            Assert.IsFalse(board.Install(PoisonSlotOne, UnknownAugment));
            Assert.IsTrue(board.Find(PoisonSlotOne)?.IsEmpty);
        }

        [TestMethod]
        public void ABoardWithoutACatalogHoldsSlotsAndJudgesNothing()
        {
            // The seam stated out loud: the rule reads records, and a composition that mints no
            // augments (the battle sandbox) has none. Such a board is the plain slot-holder it was
            // before there was a rule — this augment is three tiers above the slot.
            var board = new AbilitySocketBoard();
            board.Sync([Slot(PoisonSlotOne, PoisonAbility, tier: 1)]);

            Assert.IsTrue(board.Install(PoisonSlotOne, HighAugment));
        }

        [TestMethod]
        public void TheBoardStillResolvesWhereNothingRegistersACatalog()
        {
            // Every project composes the board by type, and the catalog is optional: a container that
            // cannot build the board takes every socket in the game down with it.
            var services = new ServiceCollection();
            services.AddSingleton<IAbilitySocketBoard, AbilitySocketBoard>();

            var board = services.BuildServiceProvider().GetRequiredService<IAbilitySocketBoard>();

            Assert.AreEqual(0, board.Sockets.Count);
        }

        [TestMethod]
        public void TheVerdictNamesTheRuleThatRefused()
        {
            // The refusals are told apart so a window can say why, instead of working the rule out
            // a second time in order to explain it.
            string[] poison = [AbilityTags.Poison];
            AbilitySocketPlacement slot = Slot(PoisonSlotOne, PoisonAbility, tier: 2);

            Assert.AreEqual(AugmentFitResult.Fits,
                AugmentFit.Check(slot, poison, Augment(EqualAugment, tier: 2, poison), []));
            Assert.AreEqual(AugmentFitResult.TierAboveSocket,
                AugmentFit.Check(slot, poison, Augment(HighAugment, tier: 3, poison), []));
            Assert.AreEqual(AugmentFitResult.NoSharedTag,
                AugmentFit.Check(slot, poison, Augment(ColdAugment, tier: 1, [AbilityTags.Cold]), []));
            Assert.AreEqual(AugmentFitResult.BoundToAnotherAbility,
                AugmentFit.Check(slot, poison, Augment(BoundAugment, tier: 1, poison, abilityId: ColdAbility), []));
            Assert.AreEqual(AugmentFitResult.ExclusionGroupTaken,
                AugmentFit.Check(slot, poison, Augment(GroupedAugment, tier: 1, poison, exclusionGroup: DurationGroup), [DurationGroup]));
        }

        [TestMethod]
        public void ARecordThatNamesNoRarityIsThePlainestAugmentThereIs()
        {
            // The rarity scale is shared with items, and its zero is Legendary: a record that says
            // nothing must not read as the best augment in the game.
            Assert.AreEqual(Rarity.Common, new AbilityUpgradeData().Rarity);
        }

        [TestMethod]
        public void ARecordCarriesNoBindingNoPoolAndNoGroupUntilItSaysSo()
        {
            var declared = new AbilityUpgradeData();

            Assert.AreEqual(string.Empty, declared.AbilityId, "an augment is bound to an ability it never named");
            Assert.AreEqual(string.Empty, declared.DropPool, "an augment drops from a pool it never named");
            Assert.AreEqual(string.Empty, declared.ExclusionGroup, "an augment conflicts with a group it never named");
        }

        /// <summary>
        /// The rule is one predicate or it is not a rule. A second tier comparison — in the board, in
        /// a socket window, in whatever judges a conversion — is a second answer to the same
        /// question, and two answers agree only until one of them is edited: an augment the window
        /// offers and the board refuses. The audit walks the shipped sources: no file that speaks
        /// about the slots or about the records may hold tiers against each other or intersect tags
        /// on its own, and the predicate itself must still do both, so the invariant cannot be
        /// satisfied by deleting the rule.
        /// </summary>
        [TestMethod]
        public void TheFittingRuleIsWrittenInOnePlace()
        {
            string predicate = Path.Combine(SrcRoot, "Core", "Battle", "Abilities", "AugmentFit.cs");
            Assert.IsTrue(File.Exists(predicate), $"the predicate is not where the rule lives: {predicate}");

            string[] rule = File.ReadAllLines(predicate);
            Assert.IsTrue(rule.Any(s_tierComparison.IsMatch), "the predicate holds no tier against another — the tier rule is gone");
            Assert.IsTrue(rule.Any(line => s_tagComparisons.Any(comparison => line.Contains(comparison, StringComparison.Ordinal))),
                "the predicate compares no tags — the tag rule is gone");

            foreach (string source in ShippedSources().Where(path => !string.Equals(path, predicate, StringComparison.OrdinalIgnoreCase)))
            {
                string[] lines = File.ReadAllLines(source);
                if (!lines.Any(MentionsTheSubsystem)) continue;

                foreach (string line in lines)
                {
                    Assert.IsFalse(s_tierComparison.IsMatch(line),
                        $"{Path.GetRelativePath(SrcRoot, source)}: holds tier against tier itself — '{line.Trim()}'");
                    Assert.IsFalse(s_tagComparisons.Any(comparison => line.Contains(comparison, StringComparison.Ordinal)),
                        $"{Path.GetRelativePath(SrcRoot, source)}: compares tags itself — '{line.Trim()}'");
                }
            }
        }

        /// <summary>Every source file the game ships, build output and the tests themselves aside.</summary>
        private static IEnumerable<string> ShippedSources() =>
            Directory.EnumerateFiles(SrcRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(SrcRoot, path)
                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(folder => s_generatedFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)));

        private static bool MentionsTheSubsystem(string line) =>
            s_subsystemMarkers.Any(marker => line.Contains(marker, StringComparison.Ordinal));

        private static AbilitySocketPlacement Slot(string socketId, string abilityId, int tier) =>
            new(socketId, abilityId, tier);

        /// <summary>One augment record as its data would declare it.</summary>
        private static AbilityUpgradeData Augment(
            string id,
            int tier,
            string[] tags,
            string abilityId = "",
            string exclusionGroup = "") =>
            new()
            {
                Id = id,
                Tier = tier,
                Tags = tags,
                AbilityId = abilityId,
                ExclusionGroup = exclusionGroup
            };

        /// <summary>A catalog holding one poison ability and nothing else yet.</summary>
        private static AugmentCatalogStub PoisonCatalog() =>
            new AugmentCatalogStub().WithAbility(PoisonAbility, AbilityTags.Poison, AbilityTags.Attack);

        private static AbilitySocketBoard BoardOver(AugmentCatalogStub catalog, params AbilitySocketPlacement[] slots)
        {
            var board = new AbilitySocketBoard(catalog);
            board.Sync(slots);
            return board;
        }

        /// <summary>Stands in for the data pipeline: the records are already parsed.</summary>
        private sealed class AugmentCatalogStub : IAbilityAugmentCatalog
        {
            private readonly Dictionary<string, AbilityUpgradeData> _augments = new(StringComparer.Ordinal);
            private readonly Dictionary<string, string[]> _abilityTags = new(StringComparer.Ordinal);

            public AugmentCatalogStub WithAbility(string abilityId, params string[] tags)
            {
                _abilityTags[abilityId] = tags;
                return this;
            }

            public AugmentCatalogStub With(AbilityUpgradeData augment)
            {
                _augments[augment.Id] = augment;
                return this;
            }

            public AbilityUpgradeData? Find(string augmentId) => _augments.GetValueOrDefault(augmentId);

            public IReadOnlyCollection<string> TagsOf(string abilityId) => _abilityTags.GetValueOrDefault(abilityId, []);
        }
    }
}
