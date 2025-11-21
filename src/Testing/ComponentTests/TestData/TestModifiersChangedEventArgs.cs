namespace LastBreathTest.ComponentTests
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Components;

    public class TestModifiersChangedEventArgs(EntityParameter parameter, IReadOnlyList<IModifierInstance> modifiers) : EventArgs, IModifiersChangedEventArgs
    {
        public EntityParameter EntityParameter { get; } = parameter;
        public IReadOnlyList<IModifierInstance> Modifiers { get; } = modifiers;
    }
}
