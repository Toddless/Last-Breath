namespace LastBreathTest.BattleSystemTests
{
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Modifiers.Context;
    using Moq;

    /// <summary>The data path of a context line ends in a binding: a knob without one throws the moment an
    /// item carrying it is equipped. This is the guard that adding a <see cref="ContextParameter"/> member
    /// without wiring it never ships.</summary>
    [TestClass]
    public class ContextModifierBindingTests
    {
        [TestMethod]
        public void TheSwitchKnobsAreExactlyTheOnesWhoseBindingReadsNoValue()
        {
            // Switch-ness is not declared anywhere: it is read off the binding table, where a knob that
            // carries a number takes a live view of the line's value and a switch takes none. Pinning the
            // answer here is what makes the derivation reviewable — a second switch, or a value view
            // dropped from a knob that has one, changes this list and has to be argued for rather than
            // slipping in as data that authors "+100% for free" the moment someone writes "flag" on it.
            CollectionAssert.AreEquivalent(
                new[] { ContextParameter.AttacksIgnoreResistances },
                ContextKnobs.Flags.ToArray(),
                "the switch knobs changed: " + string.Join(", ", ContextKnobs.Flags));

            // Both halves of the rule the two readers enforce, on the two kinds of knob.
            Assert.IsNull(ContextKnobs.WhyRefused(ContextParameter.AttacksIgnoreResistances, ModifierValueType.Flag));
            Assert.IsNull(ContextKnobs.WhyRefused(ContextParameter.HealingEfficiency, ModifierValueType.Increase));
            Assert.IsNotNull(ContextKnobs.WhyRefused(ContextParameter.AttacksIgnoreResistances, ModifierValueType.Increase));
            Assert.IsNotNull(ContextKnobs.WhyRefused(ContextParameter.HealingEfficiency, ModifierValueType.Flag));
        }

        [TestMethod]
        public void AKnobNobodyWiredRefusesEveryLine()
        {
            // The readers ask one question before they build a line, so it has to answer for the knob's
            // existence too and not only for its kind. A knob with no binding reaches no pipeline and
            // throws where it is attached — on equip, half a game away from the pool that named it — and
            // "not a switch" is exactly what an unwired knob looks like to the kind half of the question.
            // A value outside the enum stands in here for the member that arrives without a binding.
            const ContextParameter unwired = (ContextParameter)0;

            Assert.IsFalse(ContextKnobs.IsBound(unwired));
            Assert.IsNotNull(ContextKnobs.WhyRefused(unwired, ModifierValueType.Increase));
            Assert.IsNotNull(ContextKnobs.WhyRefused(unwired, ModifierValueType.Flag));
        }

        [TestMethod]
        public void EveryContextParameter_HasABinding()
        {
            foreach (var parameter in Enum.GetValues<ContextParameter>())
            {
                var entry = new ContextModifierEntry(parameter, ModifierValueType.Increase, 0.5f);
                var owner = Owner();

                entry.Attach(owner);
                entry.Detach(owner);
            }
        }

        [TestMethod]
        public void AttachAndDetach_AddAndRemoveTheSameInstance()
        {
            var handler = new Mock<IModifierHandlerComponent>();
            var owner = Owner(handler);
            var entry = new ContextModifierEntry(ContextParameter.DamageTakenReductionFromAttack, ModifierValueType.Increase, 0.2f);

            entry.Attach(owner);
            entry.Detach(owner);

            // Owner-gated modifiers are built at attach time, so detach must drop the instance that was added,
            // not a fresh one — a mismatch would leave the line stuck on the entity after unequip.
            handler.Verify(mock => mock.Add(It.IsAny<IDamageModifier>()), Times.Once);
            handler.Verify(mock => mock.Remove(It.IsAny<IDamageModifier>()), Times.Once);
        }

        private static IFightable Owner(Mock<IModifierHandlerComponent>? handler = null)
        {
            var owner = new Mock<IFightable>();
            owner.SetupGet(mock => mock.ModifierHandler).Returns((handler ?? new Mock<IModifierHandlerComponent>()).Object);
            owner.SetupGet(mock => mock.InstanceId).Returns("owner");
            return owner.Object;
        }
    }
}
