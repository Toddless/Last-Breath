namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.PoisonExplosion;
    using Battle.Source.Abilities.SeriesOfAttacks;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;
    using ChainLightningCast = Battle.Source.Abilities.ChainLightning.ChainLightning;
    using DeepFreezeCast = Battle.Source.Abilities.DeepFreeze.DeepFreeze;
    using IceAegisCast = Battle.Source.Abilities.IceAegis.IceAegis;
    using IceBlocksCast = Battle.Source.Abilities.IceBlock.IceBlocks;
    using IceShardsCast = Battle.Source.Abilities.IceShards.IceShards;
    using PoisonExplosionCast = Battle.Source.Abilities.PoisonExplosion.PoisonExplosion;
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
        private const int ExtraShards = 1;

        /// <summary>The shipped ability that counts its projectiles, and the shipped record that sells
        /// two more of them — the count the walk above moves by hand.</summary>
        private const string ShardsId = "Ability_Ice_Shards";
        private const string ProjectileRecord = "Augment_Additional_Projectiles";

        /// <summary>The second ability the count reaches since CL-3b — one jar by default.</summary>
        private const string JarId = "Ability_Jar_Of_Poison";

        private const int PoisonTurns = 3;

        /// <summary>The share of the blow the stack below ticks with. Nothing here measures a tick, but a
        /// stack that ticks for nothing is never laid at all, so the walk needs SOME share named.</summary>
        private const float TickShare = 0.5f;

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

            Assert.AreEqual(shards.ProjectileCount, seen.Count, "one target took a number of impacts other than the shards fired at him");
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
            Assert.AreEqual(shards.ProjectileCount * 2, volley.Count, "stage 3 fires every shard at both enemies, and that is not what landed");
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

        [TestMethod]
        public async Task TwoMoreShardsAreTwoMoreRiderApplicationsOnEveryTarget()
        {
            // The arithmetic the whole wave rests on, measured on the mechanism alone: a bare decorator
            // over the shared projectile count, no record involved. What is measured is the rider, not
            // the damage — a volley that grew by two has to hand the riders exactly two more impacts
            // per target, with no code knowing that a count and a rider were bought together. The walk
            // below buys the count with the shipped record instead, over the same delivery.
            using var rolls = new CombatRandomScope(new HighestRoll());
            var owner = Fighter();
            var target = Fighter();
            var bystander = Fighter();
            var shards = new IceShardsCast(Data());
            var seen = Riding(shards);
            shards.SetOwner(owner);
            IBattleField field = FieldOf(owner, target, bystander);
            int shardsBefore = shards.ProjectileCount;

            await shards.Execute([target, bystander], field);
            int impactsBefore = seen.Count;
            Assert.AreEqual(shardsBefore * 2, impactsBefore, "the volley did not even hand one impact per shard per target");

            seen.Clear();
            shards.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.ProjectileCount, Priority.Weak, OperationType.Add, ExtraShards,
                "Ability_Parameter_Decorator_Test_Extra_Shards", "Test"));

            await shards.Execute([target, bystander], field);

            Assert.AreEqual(shardsBefore + ExtraShards, shards.ProjectileCount, "the decorator never reached the shard count");
            Assert.AreEqual(impactsBefore + (ExtraShards * 2), seen.Count,
                "two more shards over two targets left the riders with something other than four more impacts");
            Assert.AreEqual(ExtraShards, seen.FindAll(impact => ReferenceEquals(impact.Target, target)).Count - shardsBefore,
                "the extra shards did not reach the chosen target as impacts of its own");
            foreach (AbilityImpact impact in seen)
                Assert.AreEqual(ImpactKind.Projectile, impact.Kind, "an added shard arrived as something other than a projectile");
        }

        [TestMethod]
        public async Task TheShippedProjectileRecordThrowsMoreJarsAndLaysMorePoison()
        {
            // The other half of the owner's rule "a genus tag gets a genus key with a default": the jar
            // has worn 'projectile' all along, the record fitted it all along, and until CL-3b it bought
            // nothing. What is asked is the arithmetic the rule promises — one more jar is one more
            // landing and one more poison stack — and NOT that the landing changed what it is: the jar's
            // touches are hits by decision, however many jars are thrown.
            using var rolls = new CombatRandomScope(new HighestRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var owner = Fighter();
            var target = Fighter();

            IAbility bare = registry.CreateAbility(JarId);
            int jarsBefore = (int)bare[AbilityParameter.ProjectileCount];
            Assert.AreEqual(1, jarsBefore, "the jar no longer declares the single throw the record multiplies");

            IAbility jar = registry.CreateAbility(JarId);
            var seen = Riding(jar);
            jar.SetOwner(owner);

            AbilityAugmentData? record = catalog.Find(ProjectileRecord);
            IAugment? upgrade = registry.CreateUpgrade(record!);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{ProjectileRecord}'");

            jar.InstallUpgrades(new Dictionary<string, IAugment> { ["socket_projectiles"] = upgrade });
            await jar.Execute([target], FieldOf(owner, target));

            Assert.AreEqual(jarsBefore + ExtraShards, (int)jar[AbilityParameter.ProjectileCount],
                $"'{ProjectileRecord}' did not reach the jar's throw count");
            Assert.AreEqual(jarsBefore + ExtraShards, seen.Count,
                "the extra jars were counted but never thrown — the loop does not read the count");
            Assert.AreEqual(jarsBefore + ExtraShards,
                target.Effects.GetBy(effect => effect.IsSame("Effect_Damage_Over_Turn_Poison")).Count(),
                "more jars landed and the poison did not stack with them");
            foreach (AbilityImpact impact in seen)
                Assert.AreEqual(ImpactKind.Hit, impact.Kind, "a thrown jar landed as something other than a hit");
        }

        [TestMethod]
        public async Task TheShippedProjectileRecordBuysTheSameExtraImpacts()
        {
            // The same claim with the augment the player actually holds: the record, built by the
            // registry, seated on the ability the shipped data builds. Nothing between the two walks is
            // shared — one moves the count by hand, this one buys it — so a record that names the wrong
            // key, falls back to the wrong number or is refused by the ability shows up here alone.
            using var rolls = new CombatRandomScope(new HighestRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var owner = Fighter();
            var target = Fighter();
            var bystander = Fighter();

            IAbility bare = registry.CreateAbility(ShardsId);
            int shardsBefore = (int)bare[AbilityParameter.ProjectileCount];

            IAbility shards = registry.CreateAbility(ShardsId);
            var seen = Riding(shards);
            shards.SetOwner(owner);

            AbilityAugmentData? record = catalog.Find(ProjectileRecord);
            Assert.IsNotNull(record, $"the shipped data declares no '{ProjectileRecord}'");
            IAugment? upgrade = registry.CreateUpgrade(record);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{ProjectileRecord}'");

            shards.InstallUpgrades(new Dictionary<string, IAugment> { ["socket_projectiles"] = upgrade });
            await shards.Execute([target, bystander], FieldOf(owner, target, bystander));

            Assert.AreEqual(shardsBefore + ExtraShards, (int)shards[AbilityParameter.ProjectileCount],
                $"'{ProjectileRecord}' did not reach the shared projectile count");
            Assert.AreEqual((shardsBefore + ExtraShards) * 2, seen.Count,
                "the bought projectiles did not arrive at the riders as impacts of their own");
            foreach (AbilityImpact impact in seen)
                Assert.AreEqual(ImpactKind.Projectile, impact.Kind, "a bought shard arrived as something other than a projectile");
        }

        [TestMethod]
        public async Task EveryPoisonedTargetTheExplosionBurstsReachesTheRiders()
        {
            // The explosion touches whoever carries poison — it deals their whole remaining poison at
            // once and can execute them outright — and until this pin it called no rider at all, so an
            // augment seated on it was seated on nothing.
            using var rolls = new CombatRandomScope(new HighestRoll());
            var owner = Fighter();
            var poisoned = Fighter();
            var alsoPoisoned = Fighter();
            var clean = Fighter();
            await Poison(owner, poisoned);
            await Poison(owner, alsoPoisoned);
            var explosion = new PoisonExplosionCast(Data());
            var seen = Riding(explosion);
            explosion.SetOwner(owner);

            await explosion.Execute([poisoned, alsoPoisoned, clean], FieldOf(owner, poisoned, alsoPoisoned, clean));

            Assert.AreEqual(2, seen.Count, "the explosion reached the riders a number of times other than once per poisoned target");
            Assert.IsTrue(seen.Exists(impact => ReferenceEquals(impact.Target, poisoned)), "the first poisoned target was blown up unnoticed");
            Assert.IsTrue(seen.Exists(impact => ReferenceEquals(impact.Target, alsoPoisoned)), "the second poisoned target was blown up unnoticed");
            Assert.IsFalse(seen.Exists(impact => ReferenceEquals(impact.Target, clean)),
                "a target carrying no poison was reported as touched — nothing happened to him");
            foreach (AbilityImpact impact in seen)
            {
                Assert.AreEqual(ImpactKind.Hit, impact.Kind, "the burst is the cast reaching its own target — that road is a hit");
                Assert.AreSame(explosion, impact.Source, "the burst arrived without the ability that set it off");
            }
        }

        [TestMethod]
        public async Task EveryExtraBlockOfTheFinalStageIsAHitOfItsOwn()
        {
            // Stage 4 drops more blocks: its own count, its own share of the damage and — with the L3
            // upgrade — its own choice of victim. That is a delivery of the ability, not something spilled
            // by the block that landed, so each one is a hit and an augment adding blocks buys rider work
            // by arithmetic. The lowest roll is what opens stage 4 at all.
            // The count below holds because the drop is started and not awaited (`_ =`, the shrapnel
            // precedent) and everything it awaits completes synchronously here — the same footing the
            // shrapnel pin stands on. A delivery that ever waits for real would need this pin rewritten.
            using var rolls = new CombatRandomScope(new LowestRoll());
            var owner = Fighter();
            var target = Fighter();
            var blocks = new IceBlocksCast(Data());
            var seen = Riding(blocks);
            blocks.SetOwner(owner);

            await blocks.Execute([target], FieldOf(owner, target));

            Assert.AreEqual(1 + blocks.ExtraBlocks, seen.Count,
                "the block that was aimed and the blocks that followed it did not add up to one impact each");
            foreach (AbilityImpact impact in seen)
            {
                Assert.AreEqual(ImpactKind.Hit, impact.Kind, "a block crashed down as something other than a hit");
                Assert.AreSame(blocks, impact.Source, "a block arrived without the ability that dropped it");
                Assert.AreSame(target, impact.Target, "a block landed on somebody the cast never aimed at");
            }
        }

        [TestMethod]
        public async Task ThePoisonSpreadingOffAnExplosionCatchesEveryOtherEnemyAsASplash()
        {
            // The spread reaches fighters the cast never aimed at, and it reaches them with an effect
            // rather than with damage — a landing all the same. Splash: nobody aimed it and it exists
            // only because somebody else's stacks went off. The exploded target is not among them: his
            // own poison is what is being carried away from him.
            using var rolls = new CombatRandomScope(new HighestRoll());
            var owner = Fighter();
            var poisoned = Fighter();
            var neighbour = Fighter();
            var farther = Fighter();
            await Poison(owner, poisoned);
            var explosion = new PoisonExplosionCast(Data()) { SpreadMode = new SpreadPoisonToAll() };
            var seen = Riding(explosion);
            explosion.SetOwner(owner);

            await explosion.Execute([poisoned], FieldOf(owner, poisoned, neighbour, farther));

            List<AbilityImpact> splash = seen.FindAll(impact => impact.Kind == ImpactKind.Splash);
            Assert.AreEqual(1, seen.FindAll(impact => impact.Kind == ImpactKind.Hit).Count, "the burst on the poisoned target itself went unreported");
            Assert.AreEqual(2, splash.Count, "the spread reached a number of bystanders other than the two other enemies on the field");
            Assert.IsTrue(splash.Exists(impact => ReferenceEquals(impact.Target, neighbour)), "the nearer bystander caught the poison unnoticed");
            Assert.IsTrue(splash.Exists(impact => ReferenceEquals(impact.Target, farther)), "the farther bystander caught the poison unnoticed");
            Assert.IsFalse(splash.Exists(impact => ReferenceEquals(impact.Target, poisoned)), "the target that exploded was reported as caught by his own spread");
            foreach (AbilityImpact impact in splash)
                Assert.AreSame(explosion, impact.Source, "a caught bystander arrived without the ability whose spread caught him");
        }

        [TestMethod]
        public async Task TheRandomSpreadOfAnExplosionLandsOnAnotherEnemyAndNobodyElse()
        {
            // The arena hands out a random fighter without looking at what it was asked — the caster and
            // the freshly exploded target included — and the impact this spread now reports is what
            // carries rider payload to whoever it names. So the pick has to be the cast's own: another
            // living enemy, never the caster and never the target the poison was taken from.
            using var rolls = new CombatRandomScope(new HighestRoll());
            var owner = Fighter();
            var poisoned = Fighter();
            var neighbour = Fighter();
            await Poison(owner, poisoned);
            var explosion = new PoisonExplosionCast(Data()) { SpreadMode = new SpreadPoisonToRandomTarget() };
            var seen = Riding(explosion);
            explosion.SetOwner(owner);

            await explosion.Execute([poisoned], FieldOf(owner, poisoned, neighbour));

            List<AbilityImpact> splash = seen.FindAll(impact => impact.Kind == ImpactKind.Splash);
            Assert.AreEqual(1, splash.Count, "the spread carried the poison to a number of fighters other than one");
            Assert.AreSame(neighbour, splash[0].Target, "the spread landed on the caster or on the victim it was taken from");
            Assert.AreSame(explosion, splash[0].Source, "the spread arrived without the ability that carried it");
        }

        [TestMethod]
        public async Task TheFreezeSpreadingOffADeepFreezeCatchesOneUntouchedEnemyAsASplash()
        {
            // The L2 spread picks from the enemies the plan left OUT, so nothing aimed at him and he is
            // frozen only because the cast landed on somebody else. Damage-free again, and again a
            // landing: what makes an impact is the touch, not the payload.
            using var rolls = new CombatRandomScope(new HighestRoll());
            var owner = Fighter();
            var target = Fighter();
            var untouched = Fighter();
            var freeze = new DeepFreezeCast(Data()) { SpreadFreezeChance = 1f };
            var seen = Riding(freeze);
            freeze.SetOwner(owner);

            await freeze.Execute([target], FieldOf(owner, target, untouched));

            List<AbilityImpact> splash = seen.FindAll(impact => impact.Kind == ImpactKind.Splash);
            Assert.AreEqual(1, splash.Count, "the spread froze somebody without telling a single rider");
            Assert.AreSame(untouched, splash[0].Target, "the spread was reported on a target the plan had already frozen");
            Assert.AreSame(freeze, splash[0].Source, "the spread arrived without the ability that cast it");
            Assert.IsTrue(seen.Exists(impact => impact.Kind == ImpactKind.Hit && ReferenceEquals(impact.Target, target)),
                "the cast's own target went unreported, so the splash above proves nothing about the pair");
        }

        [TestMethod]
        public async Task TheShatteringOfTheAegisFreezesTheFieldAsReactions()
        {
            // The answer a broken guard gives whoever broke it — a reaction and not a share of a blow
            // spilling onto a bystander. It lives in the barrier effect, but the closure is the ability's own and
            // holds it, so nothing here has to wait for the ability's trail to reach effects. The
            // shattering is reproduced the way a fight does it: the bearer's barrier runs out.
            using var rolls = new CombatRandomScope(new LowestRoll());
            var owner = Fighter();
            var first = Fighter();
            var second = Fighter();
            var aegis = new IceAegisCast(Data());
            var seen = Riding(aegis);
            aegis.SetOwner(owner);

            await aegis.Execute([owner], FieldOf(owner, first, second));
            Assert.AreEqual(0, seen.Count, "the aegis touched somebody on the way up — it is a buff on its bearer and nothing else");

            owner.CurrentBarrier = 0;

            Assert.AreEqual(2, seen.Count, "the field was frozen by the shattering and no rider heard of it");
            foreach (AbilityImpact impact in seen)
            {
                Assert.AreEqual(ImpactKind.Reaction, impact.Kind, "the answer to a shattered guard did not arrive as a reaction");
                Assert.AreSame(aegis, impact.Source, "the freeze arrived without the ability whose barrier broke");
            }

            Assert.IsTrue(seen.Exists(impact => ReferenceEquals(impact.Target, first)), "the first enemy was frozen unnoticed");
            Assert.IsTrue(seen.Exists(impact => ReferenceEquals(impact.Target, second)), "the second enemy was frozen unnoticed");
        }

        /// <summary>One poison stack on the victim, laid the way any cast lays one.</summary>
        private static async Task Poison(IFightable caster, IFightable victim) =>
            await new DamageOverTurnEffect(PoisonTurns, StatusEffects.Poison, DamageOverTurnEffect.NoCeilingOfItsOwn, TickShare).Apply(new EffectApplyingContext
            {
                Caster = caster,
                Target = victim,
                Source = "Test_Poison",
                Damage = DamageSnapshot.Of(DamageType.Physical, Health)
            });


        /// <summary>Seats a rider that only remembers, and hands back what it collected.</summary>
        private static List<AbilityImpact> Riding(IAbility ability)
        {
            var capture = new CaptureRider();
            ability.AddImpactRider(capture.Id, capture);
            return capture.Impacts;
        }

        private static AbilityBaseData Data(string id = "Ability_Test_Impact") => new() { Id = id };

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Health);
            fighter.SetMaximum(EntityParameter.Mana, Health);
            // A chance the scripted draws can win and lose: a fighter without one crits on nothing at all.
            fighter.SetMaximum(EntityParameter.CriticalChance, 0.5f);
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
            // The arena's own answer to this one: ANY living fighter, the caster included, whatever it
            // was handed. A delivery that leans on it has to be tested against that answer and not
            // against a null nobody ships.
            field.Setup(battlefield => battlefield.GetRandomEntity(It.IsAny<IFightable>())).Returns(owner);

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
