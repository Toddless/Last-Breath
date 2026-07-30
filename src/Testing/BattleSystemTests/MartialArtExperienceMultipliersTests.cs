namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Moq;

    /// <summary>
    /// The experience ladders of <see cref="BattleExperienceProcessor"/>: every member of both enums
    /// is worth something, a better member is worth more than a worse one, and a member nobody put on
    /// the ladder is paid at the weakest known rate instead of silently contributing nothing.
    /// </summary>
    [TestClass]
    public class MartialArtExperienceMultipliersTests
    {
        /// <summary>Rarity is an INVERTED scale (Legendary = 0 … Common = 4), so the ladder is written
        /// by quality by hand. A new member fails the coverage test until it is placed here.</summary>
        private static readonly Rarity[] s_rarityLadder =
            [Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary, Rarity.Unique, Rarity.Mythic];

        private static readonly EntityType[] s_typeLadder =
            [EntityType.Regular, EntityType.Special, EntityType.Elit, EntityType.Unique, EntityType.Boss, EntityType.Archon];

        /// <summary>High enough that a 0.01 step of the ladder survives the rounding to whole experience.</summary>
        private const int NpcLevel = 150;

        [TestMethod]
        public void RarityLadder_CoversEveryMemberOfTheEnum()
        {
            foreach (Rarity rarity in Enum.GetValues<Rarity>())
            {
                Assert.IsTrue(s_rarityLadder.Contains(rarity),
                    $"Rarity.{rarity} is not on the experience ladder — give it a multiplier and place it by quality.");
            }
        }

        [TestMethod]
        public void EntityTypeLadder_CoversEveryMemberOfTheEnum()
        {
            foreach (EntityType type in Enum.GetValues<EntityType>())
            {
                Assert.IsTrue(s_typeLadder.Contains(type),
                    $"EntityType.{type} is not on the experience ladder — give it a multiplier and place it by danger.");
            }
        }

        [TestMethod]
        public void EveryTypeAndRarityCombination_IsWorthExperience()
        {
            foreach (EntityType type in Enum.GetValues<EntityType>())
            {
                foreach (Rarity rarity in Enum.GetValues<Rarity>())
                {
                    Assert.IsTrue(AwardedExperienceFor(type, rarity) > 0, $"{type}/{rarity} kill awarded nothing");
                }
            }
        }

        [TestMethod]
        public void Experience_GrowsWithRarity()
        {
            AssertStrictlyGrowing(s_rarityLadder.Select(rarity => (rarity.ToString(), AwardedExperienceFor(EntityType.Regular, rarity))));
        }

        [TestMethod]
        public void Experience_GrowsWithNpcType()
        {
            AssertStrictlyGrowing(s_typeLadder.Select(type => (type.ToString(), AwardedExperienceFor(type, Rarity.Common))));
        }

        [TestMethod]
        public void Boss_IsNeverCheaperThanSpecial()
        {
            int boss = AwardedExperienceFor(EntityType.Boss, Rarity.Common);
            int special = AwardedExperienceFor(EntityType.Special, Rarity.Common);

            Assert.IsTrue(boss >= special, $"a boss ({boss}) must not be worth less than a special NPC ({special})");
        }

        [TestMethod]
        public void UnknownEnumMembers_ArePaidAtTheWeakestKnownRate()
        {
            int weakestKnown = AwardedExperienceFor(EntityType.Regular, Rarity.Common);

            Assert.AreEqual(weakestKnown, AwardedExperienceFor((EntityType)200, Rarity.Common), "an unmapped EntityType dropped the bonus");
            Assert.AreEqual(weakestKnown, AwardedExperienceFor(EntityType.Regular, (Rarity)200), "an unmapped Rarity dropped the bonus");
        }

        private static void AssertStrictlyGrowing(IEnumerable<(string Name, int Experience)> ladder)
        {
            (string Name, int Experience)[] steps = [.. ladder];
            for (int i = 1; i < steps.Length; i++)
            {
                Assert.IsTrue(steps[i].Experience > steps[i - 1].Experience,
                    $"{steps[i].Name} ({steps[i].Experience}) must be worth more than {steps[i - 1].Name} ({steps[i - 1].Experience})");
            }
        }

        private static int AwardedExperienceFor(EntityType type, Rarity rarity)
        {
            var battleBus = new BattleEventBus();
            var mastery = new Mock<IMartialArtMastery>();
            int awarded = 0;
            mastery.Setup(m => m.AddExperience(It.IsAny<int>())).Callback((int amount) => awarded = amount);
            var provider = new Mock<IGameServiceProvider>();
            provider.Setup(p => p.GetService<IMartialArtMastery>()).Returns(mastery.Object);

            var player = new Mock<IFightable>();
            player.Setup(p => p.Group).Returns((IEntityGroup?)null);

            var processor = new BattleExperienceProcessor(battleBus, provider.Object, player.Object);
            battleBus.Publish(new EntityDiedEvent(CreateNpc(type, rarity).Object));
            processor.CompleteBattle(BattleResults.PlayerWon);
            processor.Dispose();

            return awarded;
        }

        private static Mock<IFightableNpc> CreateNpc(EntityType type, Rarity rarity)
        {
            var npc = new Mock<IFightableNpc>();
            npc.Setup(n => n.InstanceId).Returns(Guid.NewGuid().ToString());
            npc.Setup(n => n.IsSummon).Returns(false);
            npc.Setup(n => n.Level).Returns(NpcLevel);
            npc.Setup(n => n.EntityType).Returns(type);
            npc.Setup(n => n.Rarity).Returns(rarity);
            npc.Setup(n => n.Group).Returns((IEntityGroup?)null);
            return npc;
        }
    }
}
