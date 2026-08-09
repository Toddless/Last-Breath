namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.SeriesOfAttacks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;
    using ChainLightningCast = Battle.Source.Abilities.ChainLightning.ChainLightning;
    using IceShardsCast = Battle.Source.Abilities.IceShards.IceShards;
    using SeriesOfAttacksCast = Battle.Source.Abilities.SeriesOfAttacks.SeriesOfAttacks;

    /// <summary>
    /// The unit of a rider's work is the IMPACT, and an impact now says out loud what it is: which cast
    /// it came out of and what kind of touch it was. The kind is what riders and data-declared behaviours
    /// are meant to filter on — "attacks" and "hits" are different things to a designer — so it has to be
    /// true where it is stamped, at the delivery, and there is nothing further down that could correct it.
    /// A delivery that mislabels itself does not fail: it quietly puts every rider bought for one road onto
    /// another, and the only place that lie is visible is here.
    /// </summary>
    [TestClass]
    public class ImpactKindTests
    {
        private const float Health = 100f;

        [TestMethod]
        public async Task EveryAttackOfASeriesReachesTheRidersAsAnAttack()
        {
            // The attack pipeline is the one road the design list calls "attacks", and the whole reason a
            // kind exists at all: everything else an ability does is a hit of some sort.
            using var rolls = new CombatRandomScope(new HighestRoll());
            var owner = Fighter();
            var target = Fighter();
            var series = new SeriesOfAttacksCast(Data());
            var seen = Riding(series);
            series.SetOwner(owner);

            await new SoAsDefaultExecutionStrategy().Execute(series, owner, [target], FieldOf(owner, target));

            Assert.IsTrue(seen.Count > 0, "the series delivered nothing at all, so the kind below proves nothing");
            foreach (AbilityImpact impact in seen)
            {
                Assert.AreEqual(ImpactKind.Attack, impact.Kind, "a swing of an attack series arrived as something other than an attack");
                Assert.AreSame(series, impact.Source, "the attack arrived without the ability whose series it is");
            }
        }

        [TestMethod]
        public async Task EveryShardOfAVolleyReachesTheRidersAsItsOwnProjectile()
        {
            // Why the kind is not merely "hit": an augment that adds projectiles has to raise what the
            // riders do by arithmetic alone, which is only true if each shard arrives on its own.
            using var rolls = new CombatRandomScope(new HighestRoll());
            var owner = Fighter();
            var target = Fighter();
            var shards = new IceShardsCast(Data());
            var seen = Riding(shards);
            shards.SetOwner(owner);

            await shards.Execute([target], FieldOf(owner, target));

            Assert.AreEqual(shards.Shards, seen.Count, "one target took a number of impacts other than the shards fired at him");
            foreach (AbilityImpact impact in seen)
            {
                Assert.AreEqual(ImpactKind.Projectile, impact.Kind, "a shard arrived as something other than a projectile");
                Assert.AreSame(shards, impact.Source, "the shard arrived without the ability that fired it");
            }
        }

        [TestMethod]
        public async Task AShardBurstingOverTheFieldReachesEveryEnemyAsASplash()
        {
            // Splash is the member of the dictionary that was added beyond the four asked for, so it is
            // the one that most needs a guard. Everything about it is deliberate: the burst is NOT another
            // shard (an augment counting projectiles must not be paid for it) and NOT the plain hit of a
            // delivery (nothing aimed it — it exists only because a shard had already landed).
            // The lowest roll is what opens the branch at all: it wins every stage roll, so the cast lands
            // on stage 4 (shrapnel) with stage 3 (every enemy) under it, and it wins every crit roll, which
            // is the burst's own condition.
            using var rolls = new CombatRandomScope(new LowestRoll());
            var owner = Fighter();
            var target = Fighter();
            var bystander = Fighter();
            var shards = new IceShardsCast(Data());
            var seen = Riding(shards);
            shards.SetOwner(owner);

            await shards.Execute([target], FieldOf(owner, target, bystander));

            List<AbilityImpact> volley = seen.FindAll(impact => impact.Kind == ImpactKind.Projectile);
            List<AbilityImpact> burst = seen.FindAll(impact => impact.Kind == ImpactKind.Splash);
            Assert.AreEqual(shards.Shards * 2, volley.Count, "stage 3 fires every shard at both enemies, and that is not what landed");
            Assert.AreEqual(volley.Count * 2, burst.Count, "every crit shard bursts over the whole field — the field is two enemies wide here");
            Assert.AreEqual(seen.Count, volley.Count + burst.Count, "the cast produced an impact of some third kind");

            foreach (AbilityImpact impact in burst)
                Assert.AreSame(shards, impact.Source, "a burst arrived without the ability whose shard burst");
            Assert.IsTrue(burst.Exists(impact => ReferenceEquals(impact.Target, target)), "the burst never reached the shard's own target");
            Assert.IsTrue(burst.Exists(impact => ReferenceEquals(impact.Target, bystander)), "the burst never reached the bystander it is meant to catch");
        }

        [TestMethod]
        public async Task TheChainStrikesOnceAndJumpsForTheRest()
        {
            // The case the honesty rule was written for. A jump is not a hit: its target was picked by the
            // chain, its damage is what the falloff left, and a rider bought for hits must not read it as
            // one. True of every stage — the roll only changes how many jumps there are.
            using var rolls = new CombatRandomScope(new HighestRoll());
            var owner = Fighter();
            var target = Fighter();
            var bystander = Fighter();
            var chain = new ChainLightningCast(Data());
            var seen = Riding(chain);
            chain.SetOwner(owner);

            await chain.Execute([target], FieldOf(owner, target, bystander));

            Assert.AreEqual(1 + chain.Jumps, seen.Count, "the chain landed a number of impacts other than a strike plus its jumps");
            Assert.AreEqual(ImpactKind.Hit, seen[0].Kind, "the strike that opens the chain arrived as a jump");
            Assert.AreSame(target, seen[0].Target, "the opening strike landed on somebody other than the chosen target");
            for (int jump = 1; jump < seen.Count; jump++)
                Assert.AreEqual(ImpactKind.ChainJump, seen[jump].Kind, $"jump {jump} of the chain arrived as a plain hit");

            foreach (AbilityImpact impact in seen)
                Assert.AreSame(chain, impact.Source, "an impact of the chain arrived without the ability that cast it");
        }

        /// <summary>Seats a rider that only remembers, and hands back what it collected.</summary>
        private static List<AbilityImpact> Riding(IAbility ability)
        {
            var capture = new CaptureRider();
            ability.ImpactRiders[capture.Id] = capture;
            return capture.Impacts;
        }

        private static AbilityBaseData Data(string id = "Ability_Test_Impact") => new() { Id = id };

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Health);
            fighter.SetMaximum(EntityParameter.Mana, Health);
            fighter.CurrentHealth = Health;
            fighter.CurrentMana = Health;

            return fighter;
        }

        private static IBattleField FieldOf(IFightable owner, params IFightable[] enemies)
        {
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns(enemies);
            field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAll()).Returns([owner, .. enemies]);

            return field.Object;
        }

        /// <summary>Every chance-roll comes back at the top of the range and every count at the bottom of
        /// it: the stance roll lands on stage 1, nothing crits, and the deliveries under test are the
        /// plain ones. What is being pinned is the label a delivery writes, not what the dice said.</summary>
        private sealed class HighestRoll : IRandomNumberGenerator
        {
            public float RandFloat() => 1f;

            public float RandFloatRange(float min, float max) => max;

            public int RandIntRange(int min, int max) => min;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }

        /// <summary>The mirror of <see cref="HighestRoll"/>: every chance-roll comes back at the bottom of
        /// the range, so every stage and every crit fires. What a delivery only does at its luckiest is
        /// unreachable otherwise — and an unreachable branch stamps its kind unwatched.</summary>
        private sealed class LowestRoll : IRandomNumberGenerator
        {
            public float RandFloat() => 0f;

            public float RandFloatRange(float min, float max) => min;

            public int RandIntRange(int min, int max) => min;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }

        /// <summary>An impact rider that does nothing but remember what it was handed, in order.</summary>
        private sealed class CaptureRider : IImpactRider
        {
            public List<AbilityImpact> Impacts { get; } = [];

            public string Id => "Rider_Capture_Impacts";

            public string InstanceId { get; } = Guid.NewGuid().ToString();

            public bool IsSame(string otherId) => Id.Equals(otherId, StringComparison.Ordinal);

            public Task Apply(AbilityImpact impact)
            {
                Impacts.Add(impact);
                return Task.CompletedTask;
            }
        }
    }
}
