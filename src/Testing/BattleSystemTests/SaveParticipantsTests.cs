namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.MessageBus;
    using Core.Save.Participants;
    using Core.Services;
    using Moq;

    [TestClass]
    public class SaveParticipantsTests
    {
        [TestMethod]
        public void MasteryRoundTripsBaseLevelWithoutBonus()
        {
            var source = new MartialArtMastery(new Mock<IGameMessageBus>().Object);
            source.AddExperience(500); // enough for a few levels
            int earnedLevel = source.CurrentLevel;
            int earnedExperience = source.CurrentExperience;
            source.AddBonusLevel(); // simulates an equipment grant: must NOT be persisted

            var captured = new MasterySaveParticipant(source).Capture();

            var target = new MartialArtMastery(new Mock<IGameMessageBus>().Object);
            new MasterySaveParticipant(target).Restore(captured, savedVersion: 1);

            Assert.AreEqual(earnedLevel, target.CurrentLevel);
            Assert.AreEqual(earnedExperience, target.CurrentExperience);
            Assert.AreEqual(0, target.BonusLevel);
        }

        [TestMethod]
        public void VitalsRestoreAndZeroHealthDoesNotKill()
        {
            var player = FakeVitalsPlayer(health: 500f, barrier: 120f, mana: 300f);
            var captured = new PlayerVitalsSaveParticipant(AccessorFor(player.Object)).Capture();

            var target = FakeVitalsPlayer(health: 9999f, barrier: 0f, mana: 0f);
            new PlayerVitalsSaveParticipant(AccessorFor(target.Object)).Restore(captured, 1);

            Assert.AreEqual(500f, target.Object.CurrentHealth);
            Assert.AreEqual(120f, target.Object.CurrentBarrier);
            Assert.AreEqual(300f, target.Object.CurrentMana);

            var corrupted = FakeVitalsPlayer(health: 0f, barrier: 0f, mana: 0f);
            var zeroCapture = new PlayerVitalsSaveParticipant(AccessorFor(corrupted.Object)).Capture();
            new PlayerVitalsSaveParticipant(AccessorFor(target.Object)).Restore(zeroCapture, 1);

            Assert.AreEqual(1f, target.Object.CurrentHealth); // checkpoint saves are alive by definition
        }

        [TestMethod]
        public void AbilityBookRoundTripsLayoutUpgradesAndStance()
        {
            // Source: two dex abilities (B stays in slot 1, A moved to slot 2), one str, upgrade chosen on A.
            var sourceBook = NewBook();
            var abilityA = FakeAbility("Ability_A");
            var abilityB = FakeAbility("Ability_B");
            var abilityC = FakeAbility("Ability_C");
            var upgrade = new Mock<IAbilityUpgrade>();
            upgrade.SetupGet(u => u.Id).Returns("A_Upgrade_L2");
            abilityA.SetupGet(a => a.CurrentUpgrades).Returns(new Dictionary<int, IAbilityUpgrade> { [2] = upgrade.Object });
            sourceBook.Learn(Stance.Dexterity, abilityA.Object);
            sourceBook.Learn(Stance.Dexterity, abilityB.Object);
            sourceBook.Learn(Stance.Strength, abilityC.Object);
            sourceBook.Equip(Stance.Dexterity, abilityA.Object.InstanceId, 2);
            sourceBook.SetStance(Stance.Strength);

            var captured = new AbilityBookSaveParticipant(AccessorFor(PlayerWithBook(sourceBook))).Capture();

            // Target: the abilities are already in the book â€” the passive tree hands them over on its
            // own section, which restores first. This one only arranges what is there.
            var targetBook = NewBook();
            var restoredAbilities = new Dictionary<string, Mock<IAbility>>();
            foreach ((string id, Stance stance) in new[] { ("Ability_A", Stance.Dexterity), ("Ability_B", Stance.Dexterity), ("Ability_C", Stance.Strength) })
                targetBook.Learn(stance, (restoredAbilities[id] = FakeAbility(id)).Object);

            var participant = new AbilityBookSaveParticipant(AccessorFor(PlayerWithBook(targetBook)));
            participant.Restore(captured, participant.Version);

            var layout = targetBook.GetSlotLayout(Stance.Dexterity);
            Assert.IsNull(layout[0]);
            Assert.AreEqual("Ability_B", layout[1]?.Id);
            Assert.AreEqual("Ability_A", layout[2]?.Id);

            restoredAbilities["Ability_A"].Verify(a => a.SelectUpgrade(2, "A_Upgrade_L2"), Times.Once);
            Assert.AreEqual(Stance.Strength, targetBook.CurrentStance);
        }

        [TestMethod]
        public void AbilityBookLeavesASlotEmptyWhenTheBookDoesNotHoldItsAbility()
        {
            // The file names an ability on the bar that no taken node grants any more: the layout
            // cannot conjure it back, it can only leave the slot empty.
            var sourceBook = NewBook();
            sourceBook.Learn(Stance.Dexterity, FakeAbility("Ability_Removed").Object);
            var captured = new AbilityBookSaveParticipant(AccessorFor(PlayerWithBook(sourceBook))).Capture();

            var targetBook = NewBook();

            var participant = new AbilityBookSaveParticipant(AccessorFor(PlayerWithBook(targetBook)));
            participant.Restore(captured, participant.Version);

            Assert.AreEqual(0, targetBook.GetAbilities(Stance.Dexterity).Count);
            Assert.IsNull(targetBook.GetSlotLayout(Stance.Dexterity)[0]);
        }

        private static AbilityBookComponent NewBook() => new(new Mock<IFightable>().Object);

        private static IPlayer PlayerWithBook(AbilityBookComponent book)
        {
            var player = new Mock<IPlayer>();
            player.SetupGet(p => p.AbilityBook).Returns(book);
            return player.Object;
        }

        private static IPlayerAccessor AccessorFor(IPlayer player)
        {
            var accessor = new Mock<IPlayerAccessor>();
            accessor.SetupGet(a => a.Player).Returns(player);
            return accessor.Object;
        }

        private static Mock<IPlayer> FakeVitalsPlayer(float health, float barrier, float mana)
        {
            var player = new Mock<IPlayer>();
            player.SetupProperty(p => p.CurrentHealth, health);
            player.SetupProperty(p => p.CurrentBarrier, barrier);
            player.SetupProperty(p => p.CurrentMana, mana);
            return player;
        }

        private static Mock<IAbility> FakeAbility(string id)
        {
            string instanceId = Guid.NewGuid().ToString();
            var ability = new Mock<IAbility>();
            ability.SetupGet(a => a.Id).Returns(id);
            ability.SetupGet(a => a.InstanceId).Returns(instanceId);
            ability.Setup(a => a.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
            ability.SetupGet(a => a.CurrentUpgrades).Returns(new Dictionary<int, IAbilityUpgrade>());
            return ability;
        }
    }
}
