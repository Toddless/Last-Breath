namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Godot;
    using Moq;

    [TestClass]
    public class AttackContextSchedulerTests
    {
        [TestMethod]
        public async Task DrainsInFifoOrder()
        {
            var scheduler = new AttackContextScheduler(_ => { });
            var first = Context(scheduler);
            var second = Context(scheduler);
            scheduler.Schedule(first);
            scheduler.Schedule(second);

            var processed = await Drain(scheduler);

            CollectionAssert.AreEqual(new[] { first, second }, processed.ToArray());
        }

        [TestMethod]
        public async Task ReactionScheduledMidDrainIsProcessedInTheSameDrain()
        {
            var scheduler = new AttackContextScheduler(_ => { });
            var reactor = Fighter();
            var attack = Context(scheduler, target: reactor);
            // The target reacts to the incoming attack (counterattack pattern).
            reactor.Setup(f => f.ReceiveAttack(It.IsAny<IAttackContext>()))
                .Callback<IAttackContext>(incoming => incoming.CreateReaction(incoming.Target, incoming.Attacker, 10f).Schedule())
                .Returns(Task.CompletedTask);
            attack.Schedule();

            var processed = await Drain(scheduler);

            Assert.AreEqual(2, processed.Count);
            Assert.AreEqual(1, processed[1].ReactionDepth);
        }

        [TestMethod]
        public async Task OverDeepReactionIsRefused()
        {
            var scheduler = new AttackContextScheduler(_ => { });
            var overDeep = Context(scheduler, reactionDepth: 25);
            scheduler.Schedule(overDeep);

            var processed = await Drain(scheduler);

            Assert.AreEqual(0, processed.Count);
        }

        [TestMethod]
        public async Task MutualReactionPingPongIsStoppedByDepthFuse()
        {
            var scheduler = new AttackContextScheduler(_ => { });
            var brawlerA = Fighter();
            var brawlerB = Fighter();
            // Both always counter: without the fuse this drain never terminates.
            AlwaysCounter(brawlerA);
            AlwaysCounter(brawlerB);
            Context(scheduler, attacker: brawlerA, target: brawlerB).Schedule();

            var processed = await Drain(scheduler);

            // Depth 0..24 resolve, depth 25 is refused: the battle ends instead of hanging.
            Assert.AreEqual(25, processed.Count);
        }

        [TestMethod]
        public async Task CancelledDrainDoesNotLeakAttacksIntoTheNextOne()
        {
            var scheduler = new AttackContextScheduler(_ => { });
            Context(scheduler).Schedule();
            Context(scheduler).Schedule();
            Context(scheduler).Schedule();

            using var cts = new CancellationTokenSource();
            await foreach (var _ in scheduler.RunQueue(cts.Token))
            {
                await cts.CancelAsync(); // consumer aborts after the first attack
            }

            var leftover = await Drain(scheduler);
            Assert.AreEqual(0, leftover.Count);
        }

        private static async Task<List<IAttackContext>> Drain(AttackContextScheduler scheduler)
        {
            var processed = new List<IAttackContext>();
            await foreach (var context in scheduler.RunQueue())
                processed.Add(context);
            return processed;
        }

        private static Mock<IFightable> Fighter()
        {
            var fighter = new Mock<IFightable>();
            fighter.SetupGet(f => f.IsAlive).Returns(true);
            fighter.Setup(f => f.Attack(It.IsAny<IAttackContext>())).Returns(Task.CompletedTask);
            fighter.Setup(f => f.ReceiveAttack(It.IsAny<IAttackContext>())).Returns(Task.CompletedTask);
            return fighter;
        }

        private static void AlwaysCounter(Mock<IFightable> fighter) =>
            fighter.Setup(f => f.ReceiveAttack(It.IsAny<IAttackContext>()))
                .Callback<IAttackContext>(incoming => incoming.CreateReaction(incoming.Target, incoming.Attacker, 10f).Schedule())
                .Returns(Task.CompletedTask);

        private static FakeAttackContext Context(
            IAttackContextScheduler scheduler,
            Mock<IFightable>? attacker = null,
            Mock<IFightable>? target = null,
            int reactionDepth = 0) =>
            new()
            {
                Attacker = (attacker ?? Fighter()).Object,
                Target = (target ?? Fighter()).Object,
                AttackContextScheduler = scheduler,
                ReactionDepth = reactionDepth
            };

        /// <summary>Pure-C# stand-in for AttackContext (the real one drags Godot's rng into the ctor).</summary>
        private sealed class FakeAttackContext : IAttackContext
        {
            private readonly Dictionary<DamageType, float> _damageComponents = [];
            public RandomNumberGenerator Rnd => null!;
            public required IFightable Attacker { get; init; }
            public required IFightable Target { get; init; }
            public required IAttackContextScheduler AttackContextScheduler { get; init; }
            public AttackResults Result { get; set; }
            public float BaseDamage { get; init; }
            public IReadOnlyDictionary<DamageType, float> DamageComponents => _damageComponents;
            public float TotalDamage => _damageComponents.Values.Sum();
            public float RawCriticalChance { get; set; }
            public float RawCriticalDamage { get; set; }
            public float RawAccuracy { get; set; }
            public DamageSnapshot FinalDamage { get; set; }
            public bool IsCritical { get; set; }
            public bool ForceCriticalAttack { get; set; }
            public bool IsUnevadable { get; set; }
            public bool IsUnblockable { get; set; }
            public bool IsValid => Attacker.IsAlive && Target.IsAlive;
            public string? SourceAbilityId { get; set; }
            public int Index { get; set; }
            public int TotalCount { get; set; } = 1;
            public bool IsFirst => Index == 0;
            public bool IsLast => Index == TotalCount - 1;
            public int ReactionDepth { get; init; }

            public void AddDamage(DamageType type, float amount) => _damageComponents[type] = _damageComponents.GetValueOrDefault(type, 0f) + amount;

            public void SetDamage(DamageType type, float amount) => _damageComponents[type] = amount;

            public void ScaleDamage(float factor)
            {
                foreach (DamageType type in _damageComponents.Keys.ToArray())
                    _damageComponents[type] *= factor;
            }

            public IAttackContext CreateReaction(IFightable attacker, IFightable target, float baseDamage) =>
                new FakeAttackContext
                {
                    Attacker = attacker,
                    Target = target,
                    AttackContextScheduler = AttackContextScheduler,
                    BaseDamage = baseDamage,
                    ReactionDepth = ReactionDepth + 1
                };

            public bool Schedule()
            {
                if (!Attacker.IsAlive || !Target.IsAlive) return false;
                AttackContextScheduler.Schedule(this);
                return true;
            }
        }
    }
}
