namespace LastBreathTest.BattleSystemTests
{
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Moq;

    /// <summary>The data path of a context line ends in a binding: a knob without one throws the moment an
    /// item carrying it is equipped. This is the guard that adding a <see cref="ContextParameter"/> member
    /// without wiring it never ships.</summary>
    [TestClass]
    public class ContextModifierBindingTests
    {
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
