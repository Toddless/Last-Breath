namespace Core.Modifiers
{
    using Enums;

    public class Modifier(ModifierValueType valueType, EntityParameter entityParameter, float baseValue, float weight = 0) : IModifier
    {
        public ModifierValueType ModifierValueType { get; } = valueType;
        public EntityParameter EntityParameter { get; } = entityParameter;
        public float BaseValue { get; } = baseValue;
        public float Value { get; set; } = baseValue;
        public float Weight { get; set; } = weight;
    }
}
