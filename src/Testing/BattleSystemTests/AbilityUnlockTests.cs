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

        private static readonly string[] s_visibleAbilities = ["Ability_Dex", "Ability_Str", "Ability_Int"];

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

        private static string[] LearnedIds(IAbilityBookComponent book) =>
            book.AllAbilities.Select(ability => ability.Id).ToArray();

        private static AbilityBookComponent NewBook() => new(new Mock<IFightable>().Object);

        private static MartialArtMastery NewMastery() => new(new Mock<IGameMessageBus>().Object);

        /// <summary>No mastery argument on purpose: the service must not be able to see a level.</summary>
        private static AbilityUnlockService CreateService(IAbilityBookComponent book)
        {
            var player = new Mock<IPlayer>();
            player.SetupGet(p => p.AbilityBook).Returns(book);
            var accessor = new Mock<IPlayerAccessor>();
            accessor.SetupGet(a => a.Player).Returns(player.Object);

            return new AbilityUnlockService(
                accessor.Object,
                new FakeAbilityProvider(),
                new Mock<IGameMessageBus>().Object,
                new Mock<ILoadScope>().Object);
        }

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
