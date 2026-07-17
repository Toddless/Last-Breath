namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle;
    using Core.Data;
    using Core.Data.LootTable;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.MessageBus;
    using Core.Services;
    using LootGeneration.Source;
    using Moq;

    /// <summary>
    /// "A summon gives NOTHING": the experience processor ignores summon deaths and flights, and
    /// the loot pipeline refuses to even ask for a summon's loot table. The identical non-summon
    /// twin earns/drops in both cases — the gate is the flag, not the setup.
    /// </summary>
    [TestClass]
    public class SummonRewardGatesTests
    {
        // ---------- experience ----------

        [TestMethod]
        public void Experience_SummonDeath_AwardsNothing()
        {
            Assert.AreEqual(0, AwardedExperienceFor(isSummon: true));
        }

        [TestMethod]
        public void Experience_RegularNpcDeath_StillAwards()
        {
            Assert.IsTrue(AwardedExperienceFor(isSummon: false) > 0, "the control NPC must be worth experience");
        }

        private static int AwardedExperienceFor(bool isSummon)
        {
            var battleBus = new BattleEventBus();
            var mastery = new Mock<IMartialArtMastery>();
            int awarded = -1;
            mastery.Setup(m => m.AddExperience(It.IsAny<int>())).Callback((int amount) => awarded = amount);
            var provider = new Mock<IGameServiceProvider>();
            provider.Setup(p => p.GetService<IMartialArtMastery>()).Returns(mastery.Object);

            var player = new Mock<IFightable>();
            player.Setup(p => p.Group).Returns((IEntityGroup?)null);

            var processor = new BattleExperienceProcessor(battleBus, provider.Object, player.Object);
            battleBus.Publish(new EntityDiedEvent(CreateNpc(isSummon).Object));
            processor.CompleteBattle(BattleResults.PlayerWon);

            processor.Dispose();
            return awarded;
        }

        // ---------- loot ----------

        [TestMethod]
        public async Task Loot_SummonDeath_DropsNothingAndNeverAsksForATable()
        {
            var messageBus = new Mock<IGameMessageBus>();
            var service = CreateLootService(messageBus);

            var items = await service.GenerateItemsAsync(CreateNpc(isSummon: true).Object);

            Assert.AreEqual(0, items.Count);
            messageBus.Verify(
                bus => bus.SendRequest<GetLootTableRequest, Dictionary<int, List<TableRecord>>>(It.IsAny<GetLootTableRequest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Loot_RegularNpcDeath_StillAsksForATable()
        {
            var messageBus = new Mock<IGameMessageBus>();
            messageBus
                .Setup(bus => bus.SendRequest<GetLootTableRequest, Dictionary<int, List<TableRecord>>>(It.IsAny<GetLootTableRequest>()))
                .ReturnsAsync([]);
            var service = CreateLootService(messageBus);

            await service.GenerateItemsAsync(CreateNpc(isSummon: false).Object);

            messageBus.Verify(
                bus => bus.SendRequest<GetLootTableRequest, Dictionary<int, List<TableRecord>>>(It.IsAny<GetLootTableRequest>()),
                Times.Once);
        }

        private static LootGenerationService CreateLootService(Mock<IGameMessageBus> messageBus)
        {
            var configuration = new Mock<ILootConfiguration>();
            configuration.Setup(c => c.TierPrices).Returns([]);
            configuration.Setup(c => c.BaseTierChances).Returns([]);
            configuration.Setup(c => c.BaseRarityChances).Returns([]);
            configuration.Setup(c => c.MaxItemsPerKill).Returns(1);
            configuration.Setup(c => c.BaseBudget).Returns([]);
            configuration.Setup(c => c.RarityMultipliers).Returns([]);

            return new LootGenerationService(
                Mock.Of<IRandomNumberGenerator>(),
                Mock.Of<IGameEventBus>(),
                messageBus.Object,
                Mock.Of<IItemCreationService>(),
                configuration.Object);
        }

        private static Mock<IFightableNpc> CreateNpc(bool isSummon)
        {
            var npc = new Mock<IFightableNpc>();
            npc.Setup(n => n.InstanceId).Returns(Guid.NewGuid().ToString());
            npc.Setup(n => n.IsSummon).Returns(isSummon);
            npc.Setup(n => n.Level).Returns(5);
            npc.Setup(n => n.EntityType).Returns(EntityType.Regular);
            npc.Setup(n => n.Rarity).Returns(Rarity.Common);
            npc.Setup(n => n.Fraction).Returns(Fractions.Undead);
            npc.Setup(n => n.Group).Returns((IEntityGroup?)null);
            npc.Setup(n => n.NpcModifiers).Returns(Mock.Of<INpcModifiersComponent>(m => m.AllModifiers == new List<INpcModifier>()));
            return npc;
        }
    }
}
