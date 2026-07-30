namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.MessageBus;
    using Core.Save;
    using Core.Services;
    using Moq;

    /// <summary>
    /// The ability book is filled by the catalog, not by the mastery level: a fresh character owns
    /// every non-hidden ability from the first frame and leveling changes nothing.
    /// </summary>
    [TestClass]
    public class AbilityUnlockTests
    {
        private const string HiddenAbility = "Ability_Boss_Reaction";
        private const string PlayerAnnouncement = "IPlayerAccessor>().Set(this)";

        private static readonly string[] s_visibleAbilities = ["Ability_Dex", "Ability_Str", "Ability_Int"];

        /// <summary>Both scene copies of the player: a contract that holds in one and not the other
        /// is the way the battle sandbox drifts away from Main.</summary>
        private static readonly string[] s_playerSources =
        [
            Path.Combine("Main", "Player", "Player.cs"),
            Path.Combine("Battle", "Internal", "Player", "Player.cs"),
        ];

        /// <summary>Components a PlayerChanged subscriber dereferences the moment it is called:
        /// the ability book (this service) and the parameter component (the world HUD).</summary>
        private static readonly string[] s_componentsReadOnAnnouncement =
        [
            "AbilityBook = new AbilityBookComponent(this)",
            "Parameters = new EntityParametersComponent()",
        ];

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
        public void NewCharacterLearnsEveryNonHiddenAbility()
        {
            var book = NewBook();
            var mastery = NewMastery(); // level 0: a character that has not fought yet

            CreateService(book);

            Assert.AreEqual(0, mastery.CurrentLevel);
            CollectionAssert.AreEquivalent(s_visibleAbilities, LearnedIds(book));
        }

        [TestMethod]
        public void MasteryLevelDoesNotChangeTheLearnedSet()
        {
            var book = NewBook();
            var mastery = NewMastery();
            var service = CreateService(book);
            string[] beforeLevelUp = LearnedIds(book);

            mastery.AddExperience(100_000);
            service.Reconcile(notify: false);

            Assert.IsTrue(mastery.CurrentLevel > 0, "the experience must have produced level ups");
            CollectionAssert.AreEquivalent(beforeLevelUp, LearnedIds(book));
        }

        [TestMethod]
        public void RepeatedReconcileDoesNotRelearnAbilities()
        {
            var book = NewBook();
            var service = CreateService(book);

            service.Reconcile(notify: false);
            service.Reconcile(notify: false);

            Assert.AreEqual(s_visibleAbilities.Length, book.AllAbilities.Count);
        }

        /// <summary>
        /// The service outlives the scene, the player does not: leaving to the menu and starting a
        /// new game builds a second player in the same process. The constructor cannot catch that one
        /// up — only the accessor's signal can, so a fresh book has to come back full.
        /// </summary>
        [TestMethod]
        public void SecondPlayerInTheSameProcessGetsHisBookFilled()
        {
            var accessor = new PlayerAccessor();
            accessor.Set(NewPlayer(NewBook()));
            CreateService(accessor); // the first scene: caught up by the constructor

            var secondBook = NewBook();
            accessor.Set(NewPlayer(secondBook));

            CollectionAssert.AreEquivalent(s_visibleAbilities, LearnedIds(secondBook));
        }

        /// <summary>
        /// The signal above is only worth having if the fighter is whole when it fires. The player
        /// is a scene node no headless test can build, so the ordering is pinned where it lives:
        /// the accessor hand-off must be the last thing _Ready does, after every component a
        /// subscriber dereferences on the spot.
        /// </summary>
        [TestMethod]
        public void PlayerAnnouncesItselfOnlyAfterItsComponentsExist()
        {
            foreach (string source in s_playerSources)
            {
                string path = Path.Combine(SrcRoot, source);
                Assert.IsTrue(File.Exists(path), $"player source not found: {path}");

                string[] lines = File.ReadAllLines(path);
                int announcement = SingleLineWith(lines, PlayerAnnouncement, source);
                foreach (string component in s_componentsReadOnAnnouncement)
                {
                    int assignment = SingleLineWith(lines, component, source);
                    Assert.IsTrue(
                        assignment < announcement,
                        $"{source}: the accessor is handed the player on line {announcement + 1}, before "
                        + $"'{component}' on line {assignment + 1} — PlayerChanged subscribers get a null field");
                }
            }
        }

        private static int SingleLineWith(string[] lines, string fragment, string source)
        {
            int[] matches = lines
                .Select((line, index) => (line, index))
                .Where(entry => entry.line.Contains(fragment, StringComparison.Ordinal))
                .Select(entry => entry.index)
                .ToArray();
            Assert.AreEqual(1, matches.Length, $"{source}: expected exactly one line with '{fragment}'");
            return matches[0];
        }

        private static string[] LearnedIds(IAbilityBookComponent book) =>
            book.AllAbilities.Select(ability => ability.Id).ToArray();

        private static AbilityBookComponent NewBook() => new(new Mock<IFightable>().Object);

        private static IPlayer NewPlayer(IAbilityBookComponent book)
        {
            var player = new Mock<IPlayer>();
            player.SetupGet(p => p.AbilityBook).Returns(book);
            return player.Object;
        }

        private static MartialArtMastery NewMastery() => new(new Mock<IGameMessageBus>().Object);

        private static AbilityUnlockService CreateService(IAbilityBookComponent book)
        {
            var accessor = new PlayerAccessor();
            accessor.Set(NewPlayer(book));
            return CreateService(accessor);
        }

        /// <summary>No mastery argument on purpose: the service must not be able to see a level.</summary>
        private static AbilityUnlockService CreateService(IPlayerAccessor accessor) =>
            new(accessor,
                new FakeAbilityProvider(),
                new Mock<IGameMessageBus>().Object,
                new Mock<ILoadScope>().Object);

        /// <summary>Catalog stub: three player abilities across the stances plus one hidden boss reaction.</summary>
        private sealed class FakeAbilityProvider : IAbilityProvider
        {
            private static readonly Dictionary<string, Stance> s_stances = new()
            {
                ["Ability_Dex"] = Stance.Dexterity,
                ["Ability_Str"] = Stance.Strength,
                ["Ability_Int"] = Stance.Intelligence,
                [HiddenAbility] = Stance.Strength,
            };

            public IReadOnlyCollection<string> KnownAbilityIds => s_stances.Keys;

            public IAbility CreateAbility(string abilityId)
            {
                string instanceId = Guid.NewGuid().ToString();
                var ability = new Mock<IAbility>();
                ability.SetupGet(a => a.Id).Returns(abilityId);
                ability.SetupGet(a => a.InstanceId).Returns(instanceId);
                ability.Setup(a => a.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
                return ability.Object;
            }

            public Stance GetAbilityStance(string abilityId) => s_stances[abilityId];

            public bool IsHidden(string abilityId) => abilityId == HiddenAbility;
        }
    }
}
