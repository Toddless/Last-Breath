namespace Core.Modifiers
{
    using Core.Enums;
    using Core.Interfaces;

    public class Modifier(ModifierType type, Parameter parameter, float baseValue) : IModifier
    {
        public ModifierType ModifierType { get; } = type;

        public Parameter Parameter { get; } = parameter;

        public float BaseValue { get; } = baseValue;

        public float Value { get; set; } = baseValue;
    }
}
