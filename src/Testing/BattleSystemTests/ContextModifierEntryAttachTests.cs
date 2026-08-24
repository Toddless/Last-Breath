namespace LastBreathTest.BattleSystemTests
{
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Moq;

    /// <summary>Binding lifecycle of a context line. A line lives on ONE owner at a time: re-attaching
    /// (a passive node taken again after a respec, an item re-equipped without an unequip) releases the
    /// previous binding first, so the entity never carries two copies of the same line and a single
    /// <see cref="ContextModifierEntry.Detach"/> always leaves it clean.</summary>
    [TestClass]
    public class ContextModifierEntryAttachTests
    {
        private const float Reduction = 0.2f;
        private const float IncomingDamage = 100f;
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void AttachTwice_ThenDetach_LeavesTheOwnerClean()
        {
            var owner = Owner();
            var entry = Entry();

            entry.Attach(owner);
            entry.Attach(owner);
            entry.Detach(owner);

            // The first binding used to be dropped without being detached: its modifier stayed on the entity
            // for the rest of the session, still reading the live line value.
            Assert.AreEqual(IncomingDamage, TakeAttack(owner), Tolerance);
        }

        [TestMethod]
        public void AttachTwice_AppliesTheLineOnce()
        {
            var owner = Owner();
            var entry = Entry();

            entry.Attach(owner);
            entry.Attach(owner);

            Assert.AreEqual(IncomingDamage * (1 - Reduction), TakeAttack(owner), Tolerance);
        }

        [TestMethod]
        public void Attach_ToASecondOwner_MovesTheLineInsteadOfDuplicatingIt()
        {
            var first = Owner("first");
            var second = Owner("second");
            var entry = Entry();

            entry.Attach(first);
            entry.Attach(second);

            Assert.AreEqual(IncomingDamage, TakeAttack(first), Tolerance);
            Assert.AreEqual(IncomingDamage * (1 - Reduction), TakeAttack(second), Tolerance);
        }

        [TestMethod]
        public void Detach_ByAPreviousOwner_LeavesTheCurrentBindingAlone()
        {
            var first = Owner("first");
            var second = Owner("second");
            var entry = Entry();

            entry.Attach(first);
            entry.Attach(second);
            entry.Detach(first);

            Assert.AreEqual(IncomingDamage * (1 - Reduction), TakeAttack(second), Tolerance);
        }

        [TestMethod]
        public void Detach_WithoutAttach_DoesNothing()
        {
            var handler = new Mock<IModifierHandlerComponent>();

            Entry().Detach(Owner(handler: handler));

            handler.Verify(mock => mock.Remove(It.IsAny<IDamageModifier>()), Times.Never);
        }

        private static ContextModifierEntry Entry() =>
            new(ContextParameter.DamageTakenReductionFromAttack, ModifierValueType.Increase, Reduction);

        /// <summary>A blow at the owner, run the way a fighter runs one: through the point that names the
        /// receiver and both modifier sides.</summary>
        private static float TakeAttack(IFightable owner)
        {
            var context = new DamageContext { Source = Owner("attacker"), Cause = DamageCause.Attack };
            context.Add(DamageType.Physical, IncomingDamage);
            Core.Calculations.ApplyDamageModifiers(context, owner);
            return context.TotalDamage;
        }

        private static IFightable Owner(string id = "owner", Mock<IModifierHandlerComponent>? handler = null)
        {
            var owner = new Mock<IFightable>();
            owner.SetupGet(mock => mock.ModifierHandler).Returns(handler?.Object ?? new ModifierHandlerComponent());
            owner.SetupGet(mock => mock.InstanceId).Returns(id);
            owner.Setup(mock => mock.IsSame(It.IsAny<string>())).Returns<string>(other => other == id);
            return owner.Object;
        }
    }
}
