namespace LastBreathTest.BattleSystemTests
{
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    /// <summary>
    /// Dispelling, both halves of it. Strength decides WHAT can be taken: a dispel takes effects at or
    /// below its own <see cref="EffectPower"/>, and an absolute effect is beyond every dispel there is —
    /// which is what makes a seal a seal. Scope decides WHICH HALF of the list is aimed at: off oneself
    /// the damaging effects and debuffs, off a target the buffs, so a single operation cannot be pointed
    /// at a fighter's own buffs by mistake.
    ///
    /// An effect ticking damage belongs with the debuffs even when nothing flagged it harmful — the
    /// classification default is "a buff", and a poison quietly falling on the buff side would be
    /// dispelled by the very move meant to clear it.
    /// </summary>
    [TestClass]
    public class EffectDispelTests
    {
        private const string Source = "Test_Dispel";

        [TestMethod]
        public async Task AWeakDispelTakesWeakEffectsAndLeavesStrongOnesStanding()
        {
            var victim = new Fighter();
            IEffect weak = await Landed(victim, Probe.Debuff("Effect_Dispel_Weak_Debuff", EffectPower.Weak));
            IEffect strong = await Landed(victim, Probe.Debuff("Effect_Dispel_Strong_Debuff", EffectPower.Strong));

            victim.Effects.Dispel(EffectPower.Weak, DispelScope.Self);

            CollectionAssert.AreEquivalent(new[] { strong }, victim.Standing(), "a weak dispel did not take exactly the weak effect");
        }

        [TestMethod]
        public async Task AStrongDispelTakesStrongAndWeakAlike()
        {
            var victim = new Fighter();
            await Landed(victim, Probe.Debuff("Effect_Dispel_Weak_Debuff", EffectPower.Weak));
            await Landed(victim, Probe.Debuff("Effect_Dispel_Strong_Debuff", EffectPower.Strong));

            victim.Effects.Dispel(EffectPower.Strong, DispelScope.Self);

            Assert.AreEqual(0, victim.Standing().Count, "a strong dispel left something behind");
        }

        [TestMethod]
        public async Task NoDispelTakesAnAbsoluteEffect()
        {
            var victim = new Fighter();
            IEffect sealedDebuff = await Landed(victim, Probe.Debuff("Effect_Dispel_Absolute_Debuff", EffectPower.Absolute));
            IEffect sealedBuff = await Landed(victim, Probe.Buff("Effect_Dispel_Absolute_Buff", EffectPower.Absolute));

            victim.Effects.Dispel(EffectPower.Absolute, DispelScope.Self);
            victim.Effects.Dispel(EffectPower.Absolute, DispelScope.Target);

            CollectionAssert.AreEquivalent(new[] { sealedDebuff, sealedBuff }, victim.Standing(),
                "an absolute effect was dispelled, by a dispel of its own strength at that");
        }

        [TestMethod]
        public async Task DispellingOffSelfTakesDamagingEffectsAndDebuffsAndLeavesTheBuffs()
        {
            var victim = new Fighter();
            await Landed(victim, Probe.Debuff("Effect_Dispel_Weak_Debuff", EffectPower.Weak));
            await Landed(victim, new DamagingProbe(EffectPower.Weak));
            IEffect buff = await Landed(victim, Probe.Buff("Effect_Dispel_Weak_Buff", EffectPower.Weak));

            victim.Effects.Dispel(EffectPower.Strong, DispelScope.Self);

            CollectionAssert.AreEquivalent(new[] { buff }, victim.Standing(),
                "dispelling off oneself did not take exactly the damaging effects and debuffs");
        }

        [TestMethod]
        public async Task DispellingOffTheTargetTakesTheBuffsAndNothingElse()
        {
            var victim = new Fighter();
            IEffect debuff = await Landed(victim, Probe.Debuff("Effect_Dispel_Weak_Debuff", EffectPower.Weak));
            IEffect damaging = await Landed(victim, new DamagingProbe(EffectPower.Weak));
            await Landed(victim, Probe.Buff("Effect_Dispel_Weak_Buff", EffectPower.Weak));

            victim.Effects.Dispel(EffectPower.Strong, DispelScope.Target);

            CollectionAssert.AreEquivalent(new[] { debuff, damaging }, victim.Standing(),
                "dispelling off a target took something other than the buffs");
        }

        [TestMethod]
        public async Task RemovingEveryEffectOfOneSourceSurvivesTheSecondEffect()
        {
            // The removal walks the very list each removal shortens: with two effects behind one source
            // the walk used to throw on the second, so a source could only ever be cleaned up when it
            // happened to have laid a single effect.
            var victim = new Fighter();
            await Landed(victim, Probe.Debuff("Effect_Dispel_Weak_Debuff", EffectPower.Weak));
            await Landed(victim, Probe.Buff("Effect_Dispel_Weak_Buff", EffectPower.Weak));

            victim.Effects.RemoveEffectBySource(Source);

            Assert.AreEqual(0, victim.Standing().Count, "an effect of the removed source stayed on the victim");
        }

        /// <summary>An effect that has actually gone through the application pipeline: removal reaches the
        /// component through the effect itself, so a bare instance in the list would not answer a dispel.</summary>
        private static async Task<IEffect> Landed(Fighter victim, IEffect effect)
        {
            await effect.Apply(new EffectApplyingContext { Caster = new Fighter().Object, Target = victim.Object, Source = Source });
            Assert.IsTrue(victim.Standing().Contains(effect), $"'{effect.Id}' never landed, so the walk proves nothing");
            return effect;
        }

        /// <summary>An effect of a chosen strength and a chosen side, wearing an id the canon carries no
        /// row for: the strength under test is the one the walk sets, not one the shipped file happens to say.</summary>
        private class Probe(string id, EffectPower power, bool harmful) : Effect(id, duration: 3, maxStacks: 1)
        {
            public static Probe Debuff(string id, EffectPower power) => new(id, power, harmful: true);
            public static Probe Buff(string id, EffectPower power) => new(id, power, harmful: false);

            public override bool IsHarmful => harmful;
            public override EffectPower Power => power;
            public override IEffect Copy() => new Probe(Id, power, harmful);
        }

        /// <summary>A damage-over-turn effect nothing flagged harmful — the case the buff/debuff default
        /// gets wrong on its own.</summary>
        private sealed class DamagingProbe(EffectPower power)
            : Probe("Effect_Dispel_Damaging", power, harmful: false), IDamageOverTurnEffect
        {
            public float DamagePerTick { get; set; }

            public override IEffect Copy() => new DamagingProbe(power);
        }

        /// <summary>A fightable an effect can land on: real effects and modifier pipelines, only the
        /// entity itself is a mock.</summary>
        private sealed class Fighter
        {
            private readonly Mock<IFightable> _mock = new();

            public IFightable Object => _mock.Object;
            public IEffectsComponent Effects { get; }

            public Fighter()
            {
                Effects = new EffectsComponent(_mock.Object);
                _mock.Setup(fighter => fighter.InstanceId).Returns(Guid.NewGuid().ToString());
                _mock.Setup(fighter => fighter.IsAlive).Returns(true);
                _mock.Setup(fighter => fighter.Effects).Returns(Effects);
                _mock.Setup(fighter => fighter.ModifierHandler).Returns(new ModifierHandlerComponent());
                _mock.Setup(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                _mock.Setup(fighter => fighter.Parameters).Returns(Mock.Of<IEntityParametersComponent>());
                _mock.Setup(fighter => fighter.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
            }

            public List<IEffect> Standing() => [.. Effects.Effects];
        }
    }
}
