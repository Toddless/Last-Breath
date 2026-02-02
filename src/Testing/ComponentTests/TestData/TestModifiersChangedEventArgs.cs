namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Components;
    using Core.Modifiers;

    public class TestModifiersChangedEventArgs(EntityParameter parameter, IReadOnlyList<IModifierInstance> modifiers) : EventArgs, IModifiersChangedEventArgs
    {
        public EntityParameter EntityParameter { get; } = parameter;
        public IReadOnlyList<IModifierInstance> Modifiers { get; } = modifiers;
    }
}
