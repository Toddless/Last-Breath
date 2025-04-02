namespace PlaygroundTest.ComponentTesting
{
    using Playground.Script.Effects;
    using Playground.Script.Enums;

    public class ModifierTest(Parameter parameter, ModifierType modifierType, int priority, float value) : IModifier
    {
        public Parameter Parameter { get; } = parameter;
        public ModifierType Type { get; } = modifierType;
        public int Priority { get; } = priority;
        public float Value { get; } = value;

        public float ModifyValue(float value) => Type == ModifierType.Multiplicative ? value * Value : value * Value;
    }
}
