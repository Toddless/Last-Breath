namespace LastBreathTest.Modifiers
{
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Modifiers.Context;
    using Moq;

    /// <summary>Two invariants shared by every context knob. A line's number is read when the pipeline runs,
    /// not when the item is worn — upgrading an EQUIPPED piece never re-attaches its lines. And a knob that
    /// writes a whole-turn value keeps the fraction it drops, so a chain of small bonuses lands where a single
    /// combined line would instead of rounding away to nothing.</summary>
    [TestClass]
    public class ContextKnobValueTests
    {
        [TestMethod]
        public void DamageTakenReduction_FollowsTheLineValue_WithoutReAttach()
        {
            var owner = Owner();
            var entry = new ContextModifierEntry(ContextParameter.DamageTakenReductionFromAttack, ModifierValueType.Increase, 0.2f);
            entry.Attach(owner);

            Assert.AreEqual(80f, TakeAttack(owner, 100f), 0.001f);

            // Sharpening a WORN item: EquipmentComponent runs no OnEquip/OnUnequip, only the value changes.
            entry.Value = 0.5f;

            Assert.AreEqual(50f, TakeAttack(owner, 100f), 0.001f);
        }

        [TestMethod]
        public void EffectDurationScale_TwoLines_MatchTheSingleCombinedLine()
        {
            // 4 × 1.1 × 1.1 = 4.84 and 4 × 1.21 = 4.84 — the same turn count, whatever the line count.
            Assert.AreEqual(ScaledDuration(4, lines: 1, bonus: 0.21f), ScaledDuration(4, lines: 2, bonus: 0.1f));
        }

        [TestMethod]
        public void EffectDurationScale_ChainOfLines_FollowsTheCompoundFormula()
        {
            // 4 × 1.1^20 = 26.91 → 27. Rounding every step separately swallowed the whole fraction: a 4-turn
            // effect stayed at 4 turns no matter how many lines the owner wore.
            Assert.AreEqual(27, ScaledDuration(4, lines: 20, bonus: 0.1f));

            // 10 × 1.1^20 = 67.27 → 67. Long effects always compounded — the loss was invisible on them.
            Assert.AreEqual(67, ScaledDuration(10, lines: 20, bonus: 0.1f));
        }

        [TestMethod]
        public void EffectDurationScale_HonoursWholeTurnKnobsRunningBetweenTheLines()
        {
            var effect = Effect(3);
            var context = new EffectApplicationContext(Mock.Of<IFightable>(), Mock.Of<IFightable>(), effect);

            // Applied by hand: equal priorities leave the handler's order unspecified, and this case is about
            // what a flat turn added mid-chain does to the carried fraction.
            new EffectDurationScaleContextModifier(() => 0.1f).Apply(context); // 3 → 3.3, written back as 3
            new EffectDurationBonusContextModifier(StatusEffects.Bleed, () => 1).Apply(context); // 3 → 4
            new EffectDurationScaleContextModifier(() => 0.1f).Apply(context);

            // (4 + 0.3) × 1.1 = 4.73 → 5: the flat turn is honoured and the carried fraction survives it.
            Assert.AreEqual(5, effect.Duration);
        }

        private static int ScaledDuration(int duration, int lines, float bonus)
        {
            var effect = Effect(duration);
            var context = new EffectApplicationContext(Mock.Of<IFightable>(), Mock.Of<IFightable>(), effect);
            var handler = new ModifierHandlerComponent();
            for (int i = 0; i < lines; i++)
                handler.Add(new EffectDurationScaleContextModifier(() => bonus));

            handler.Apply(context);
            return effect.Duration;
        }

        private static IEffect Effect(int duration)
        {
            var effect = new Mock<IEffect>();
            effect.SetupProperty(mock => mock.Duration, duration);
            effect.SetupGet(mock => mock.Status).Returns(StatusEffects.Bleed);
            return effect.Object;
        }

        /// <summary>A blow at the owner, run the way a fighter runs one: through the point that names the
        /// receiver and both modifier sides.</summary>
        private static float TakeAttack(IFightable owner, float damage)
        {
            var context = new DamageContext { Source = Fighter("attacker"), Cause = DamageCause.Attack };
            context.Add(DamageType.Physical, damage);
            Core.Calculations.ApplyDamageModifiers(context, owner);
            return context.TotalDamage;
        }

        private static IFightable Owner() => Fighter("owner");

        private static IFightable Fighter(string id)
        {
            var owner = new Mock<IFightable>();
            owner.SetupGet(mock => mock.ModifierHandler).Returns(new ModifierHandlerComponent());
            owner.SetupGet(mock => mock.InstanceId).Returns(id);
            owner.Setup(mock => mock.IsSame(It.IsAny<string>())).Returns<string>(other => other == id);
            return owner.Object;
        }
    }
}
