namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Skirmish;
    using Core.Components;
    using Core.Entity;
    using Core.Enums;
    using Godot;

    [TestClass]
    public class NpcSkirmishTests
    {
        [TestMethod]
        public void StrengthGrowsWithLevelRarityTypeAndHeadcount()
        {
            float weakling = SquadStrength.Calculate([Fake(level: 5)]);
            float leveled = SquadStrength.Calculate([Fake(level: 15)]);
            float rare = SquadStrength.Calculate([Fake(level: 5, rarity: Rarity.Mythic)]);
            float elite = SquadStrength.Calculate([Fake(level: 5, type: EntityType.Boss)]);
            float squad = SquadStrength.Calculate([Fake(level: 5), Fake(level: 5), Fake(level: 5)]);

            Assert.IsTrue(leveled > weakling);
            Assert.IsTrue(rare > weakling);
            Assert.IsTrue(elite > weakling);
            Assert.IsTrue(squad > weakling);
        }

        [TestMethod]
        public void PlaysExactlyConfiguredRoundsAndCompletes()
        {
            var skirmish = CreateSkirmish([Fake()], [Fake()], out var rounds, out var completions);

            for (int i = 0; i < 5; i++) skirmish.Tick(1.5f); // enough ticks for 3 one-second rolls

            Assert.AreEqual(3, rounds.Count);
            Assert.AreEqual(1, completions.Count);
            Assert.IsTrue(skirmish.IsCompleted);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rounds.Select(round => round.Number).ToArray());
        }

        [TestMethod]
        public void CompletedSkirmishIgnoresFurtherTicks()
        {
            var skirmish = CreateSkirmish([Fake()], [Fake()], out var rounds, out _);

            for (int i = 0; i < 10; i++) skirmish.Tick(2f);

            Assert.AreEqual(3, rounds.Count); // no fourth round ever
        }

        [TestMethod]
        public void WinnersAreTheSideWithMoreRoundWins()
        {
            var sideA = new[] { Fake() };
            var sideB = new[] { Fake() };
            var skirmish = CreateSkirmish(sideA, sideB, out var rounds, out _);

            while (!skirmish.IsCompleted) skirmish.Tick(2f);

            int sideAWins = rounds.Count(round => round.SideAWon);
            var expectedWinners = sideAWins >= 2 ? sideA : sideB;
            CollectionAssert.AreEqual(expectedWinners, skirmish.Winners.ToArray());
            Assert.AreEqual(2, skirmish.Winners.Count + skirmish.Losers.Count);
        }

        [TestMethod]
        public void MuchStrongerSideWinsWithSeededDice()
        {
            // An Archon squad vs a lone level-1 regular: strength ~+16 vs ~+0.1 — with these
            // seeds the dice cannot overcome the gap (d20 spread is at most 19).
            var boss = new[] { Fake(level: 150, rarity: Rarity.Mythic, type: EntityType.Archon), Fake(level: 80, type: EntityType.Boss) };
            var mob = new[] { Fake(level: 1) };
            var skirmish = new NpcSkirmish(boss, mob, new DefaultRandomNumberGenerator(seed: 7), TestConfig());

            while (!skirmish.IsCompleted) skirmish.Tick(2f);

            CollectionAssert.AreEqual(boss, skirmish.Winners.ToArray());
        }

        private static SkirmishConfig TestConfig() => new()
        {
            MinRollIntervalSeconds = 1f,
            MaxRollIntervalSeconds = 1f, // deterministic pacing for the tick assertions
        };

        private static NpcSkirmish CreateSkirmish(ISkirmishParticipant[] sideA, ISkirmishParticipant[] sideB,
            out List<SkirmishRound> rounds, out List<NpcSkirmish> completions)
        {
            var skirmish = new NpcSkirmish(sideA, sideB, new DefaultRandomNumberGenerator(seed: 42), TestConfig());
            var capturedRounds = new List<SkirmishRound>();
            var capturedCompletions = new List<NpcSkirmish>();
            skirmish.RoundResolved += capturedRounds.Add;
            skirmish.Completed += capturedCompletions.Add;
            rounds = capturedRounds;
            completions = capturedCompletions;
            return skirmish;
        }

        private static FakeParticipant Fake(int level = 5, Rarity rarity = Rarity.Uncommon, EntityType type = EntityType.Regular) =>
            new() { Level = level, Rarity = rarity, EntityType = type };

        private class FakeParticipant : ISkirmishParticipant
        {
            public string InstanceId { get; } = Guid.NewGuid().ToString();
            public Fractions Fraction { get; init; } = Fractions.Human;
            public int Level { get; init; } = 5;
            public Rarity Rarity { get; init; } = Rarity.Uncommon;
            public EntityType EntityType { get; init; } = EntityType.Regular;
            public Vector2 Position { get; } = Vector2.Zero;
            public bool IsFighting { get; set; }
            public bool IsAlive => !Defeated;
            public IEntityGroup? Group => null;
            public bool Defeated { get; private set; }
            public bool Burned { get; private set; }

            public void DefeatInWorld() => Defeated = true;

            public bool TryBurnBody()
            {
                if (!Defeated || Burned) return false;
                Burned = true;
                return true;
            }
        }
    }
}
