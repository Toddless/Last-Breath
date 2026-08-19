namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;
    using ArmageddonCast = Battle.Source.Abilities.Armageddon.Armageddon;

    /// <summary>
    /// The Armageddon crits, which it did not until CL-7d. It was the only damaging ability in the book
    /// that never rolled one — it carries the <c>critical</c> tag, the crit records seated on it and moved
    /// a key nobody registered, and there was nothing on the cast for a bonus to raise. Its delivery is
    /// direct damage rather than the attack pipeline, so the roll cannot come from an attack context and
    /// is read the way the multicast family reads it instead.
    ///
    /// Two things are claimed together, because the change is a buff to a shipped ability and the plain
    /// blow is what tells whether the buff stayed inside its own lane: the un-critted damage is still the
    /// figure the stance document names, to the float, and the crit multiplies exactly that.
    /// </summary>
    [TestClass]
    public class ArmageddonCriticalTests
    {
        private const string AbilityId = "Ability_Armageddon";

        /// <summary>The stage figures of the stance document, held by <see cref="AbilityDamageMarkupTests"/>.</summary>
        private const float StageOneFlat = 300f;
        private const float StageOneShare = 0.75f;
        private const float StageThreeFlat = 1000f;
        private const float StageThreeShare = 1.8f;

        private const float Health = 100000f;
        private const float WeaponDamage = 200f;
        private const float SpellDamage = 400f;

        /// <summary>The attacker's own crit multiplier, and the chance the threshold roll is judged on.</summary>
        private const float CriticalDamage = 2f;
        private const float CriticalChance = 0.5f;

        /// <summary>What one un-critted, un-mitigated stage-one blow comes to on the fighter below.</summary>
        private static float StageOneBlow => StageOneFlat + (WeaponDamage * StageOneShare) + (SpellDamage * StageOneShare);

        private static float StageThreeBlow => StageThreeFlat + (WeaponDamage * StageThreeShare) + (SpellDamage * StageThreeShare);

        [TestMethod]
        public async Task ThePlainBlowIsStillTheStageDamageTheDocumentNames()
        {
            // The half that must NOT have moved. A crit roll added to a delivery is one line away from
            // multiplying by the attacker's crit damage on every cast, and the reading that does it is the
            // one that also reads "chance zero" as a hit — so the plain figure is pinned to the float.
            using var rolls = new CombatRandomScope(new NoCrit());
            var owner = Fighter();
            var target = Fighter();
            ArmageddonCast cast = Shipped();
            cast.SetOwner(owner);

            await cast.Execute([target], FieldOf(owner, target));

            Assert.AreEqual(StageOneBlow, Health - target.CurrentHealth, 0.0001f,
                "the plain Armageddon no longer deals the flat damage plus both scales the document names");
        }

        [TestMethod]
        public async Task ACriticalBlowIsThatSameBlowTimesTheAttackersMultiplier()
        {
            using var rolls = new CombatRandomScope(new AlwaysCrit());
            var owner = Fighter();
            var target = Fighter();
            ArmageddonCast cast = Shipped();
            cast.SetOwner(owner);

            await cast.Execute([target], FieldOf(owner, target));

            Assert.AreEqual(StageOneBlow * CriticalDamage, Health - target.CurrentHealth, 0.0001f,
                "a critical Armageddon is not the plain blow times the attacker's critical damage");
        }

        [TestMethod]
        public async Task TheCritDamageRecordRaisesTheMultiplierTheBlowIsReadThrough()
        {
            // The key becoming real. 'Augment_Additional_Crit_Damage' was seated on this ability by the
            // 'critical' tag and moved a parameter nothing registered; the bonus is additive on top of the
            // attacker's own multiplier, the same reading the multicast family uses.
            float bare = await CritDamageOf(wearing: null);
            float worn = await CritDamageOf(wearing: "Augment_Additional_Crit_Damage");

            Assert.AreEqual(StageOneBlow * CriticalDamage, bare, 0.0001f, "the bare crit moved, so the comparison below proves nothing");
            Assert.AreEqual(StageOneBlow * (CriticalDamage + 0.35f), worn, 0.01f,
                "'Augment_Additional_Crit_Damage' is worn on the Armageddon and its critical blow came out undecorated");
        }

        [TestMethod]
        public async Task TheCritChanceRecordDecidesWhetherTheBlowCritsAtAll()
        {
            // The other half of the pair, which no damage figure can show: the roll is judged against a
            // fixed draw sitting just ABOVE the attacker's own chance, so the bare cast cannot crit and
            // the same cast wearing the record can. That is the chance key being read and nothing else.
            const float Draw = 0.6f;
            float bare = await BlowAgainstDraw(Draw, wearing: null);
            float worn = await BlowAgainstDraw(Draw, wearing: "Augment_Additional_Crit_Chance");

            Assert.AreEqual(StageOneBlow, bare, 0.0001f,
                $"a draw of {Draw} beat a chance of {CriticalChance} — the bare cast crit and the claim below is vacuous");
            Assert.AreEqual(StageOneBlow * CriticalDamage, worn, 0.0001f,
                "'Augment_Additional_Crit_Chance' is worn and the roll did not widen — the chance key is not being read");
        }

        [TestMethod]
        public async Task TheCritReachesTheRidersItUsedToReachAsAPlainHit()
        {
            // Riders bought for this cast read the impact and nothing else. It carried a hard `false` all
            // along, so every "on crit" rider seated on the Armageddon was buying nothing.
            using var rolls = new CombatRandomScope(new AlwaysCrit());
            var owner = Fighter();
            var target = Fighter();
            ArmageddonCast cast = Shipped();
            List<AbilityImpact> seen = Riding(cast);
            cast.SetOwner(owner);

            await cast.Execute([target], FieldOf(owner, target));

            AbilityImpact? hit = seen.Find(impact => impact.Kind == ImpactKind.Hit);
            Assert.IsNotNull(hit, "the cast's target was never reported to the riders");
            Assert.IsTrue(hit.IsCritical, "a critical blow reached the riders as a plain one");
            Assert.AreEqual(Health - target.CurrentHealth, hit.Damage.Total, 0.0001f,
                "the impact reported a different number than the target actually lost");
        }

        [TestMethod]
        public async Task TheCritMultipliesTheStageThreeDamageAndLeavesItsControlAlone()
        {
            // The stage-three payload is control, which is its own axis with its own records: a crit that
            // reached it would be one record buying two things. So the blow doubles and the stun does not.
            (float plainDamage, int plainStun) = await StageThree(new NoCrit());
            (float critDamage, int critStun) = await StageThree(new AlwaysCrit());

            Assert.AreEqual(StageThreeBlow, plainDamage, 0.0001f, "the plain stage-three blow is no longer the documented figure");
            Assert.AreEqual(StageThreeBlow * CriticalDamage, critDamage, 0.0001f, "the crit did not multiply the stage-three blow");
            Assert.AreEqual(plainStun, critStun, "the crit lengthened the stun — it reached the payload instead of the damage");
        }

        /// <summary>The damage of one guaranteed crit, optionally with a record worn.</summary>
        private static async Task<float> CritDamageOf(string? wearing)
        {
            using var rolls = new CombatRandomScope(new AlwaysCrit());
            return await Blow(wearing);
        }

        /// <summary>The damage of one cast whose crit roll draws a fixed number, optionally with a record worn.</summary>
        private static async Task<float> BlowAgainstDraw(float draw, string? wearing)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(draw));
            return await Blow(wearing);
        }

        private static async Task<float> Blow(string? wearing)
        {
            var owner = Fighter();
            var target = Fighter();
            ArmageddonCast cast = Shipped();
            Wear(cast, wearing);
            cast.SetOwner(owner);

            await cast.Execute([target], FieldOf(owner, target));
            return Health - target.CurrentHealth;
        }

        /// <summary>One stage-three cast: what the target lost and how long it was stunned for.</summary>
        private static async Task<(float Damage, int Stun)> StageThree(IRandomNumberGenerator draws)
        {
            using var rolls = new CombatRandomScope(draws);
            var owner = Fighter();
            var target = Fighter();
            ArmageddonCast cast = Shipped();
            cast.SetOwner(owner);
            cast.PendingStage = cast.MaxStage;

            await cast.Execute([target], FieldOf(owner, target));

            IEffect? stun = target.Effects.GetBy(effect => effect.IsSame("Effect_Stun")).FirstOrDefault();
            Assert.IsNotNull(stun, "the top stage laid no stun at all, so the payload claim proves nothing");
            return (Health - target.CurrentHealth, stun.Duration);
        }

        private static void Wear(ArmageddonCast cast, string? augmentId)
        {
            if (augmentId == null) return;

            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            AbilityAugmentData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            IAugment? upgrade = registry.CreateUpgrade(record);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{augmentId}'");
            upgrade.Apply(cast);
        }

        private static ArmageddonCast Shipped() => (ArmageddonCast)ShippedAbilityData.Abilities().CreateAbility(AbilityId);

        private static List<AbilityImpact> Riding(IAbility ability)
        {
            var capture = new CaptureRider();
            ability.AddImpactRider(capture.Id, capture);
            return capture.Impacts;
        }

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Health);
            fighter.SetMaximum(EntityParameter.Mana, Health);
            fighter.SetMaximum(EntityParameter.PhysicalDamage, WeaponDamage);
            fighter.SetMaximum(EntityParameter.SpellDamage, SpellDamage);
            fighter.SetMaximum(EntityParameter.CriticalDamage, CriticalDamage);
            fighter.SetMaximum(EntityParameter.CriticalChance, CriticalChance);
            fighter.TakesDamageForReal = true;
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
            field.Setup(battlefield => battlefield.GetRandomEntity(It.IsAny<IFightable>())).Returns(owner);
            return field.Object;
        }

        /// <summary>Every draw at the top of its range, so no chance below certainty ever passes.</summary>
        private sealed class NoCrit : Draws
        {
            public override float RandFloat() => 1f;
        }

        /// <summary>Every draw at the bottom, so the crit roll always passes.</summary>
        private sealed class AlwaysCrit : Draws
        {
            public override float RandFloat() => 0f;
        }

        /// <summary>One fixed draw, for measuring where the crit CHANCE lands rather than the damage.</summary>
        private sealed class FixedDraw(float draw) : Draws
        {
            public override float RandFloat() => draw;
        }

        private abstract class Draws : IRandomNumberGenerator
        {
            public abstract float RandFloat();

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

        /// <summary>Records every impact the delivery reports.</summary>
        private sealed class CaptureRider : IImpactRider
        {
            public List<AbilityImpact> Impacts { get; } = [];

            public string Id => "Rider_Capture_Armageddon";

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
