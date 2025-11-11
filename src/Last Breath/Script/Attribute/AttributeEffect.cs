namespace LastBreath.Script.Attribute
{
    using Core.Enums;

    public class AttributeEffect(Parameter parameter, ModifierType type, float valuePerPoint, int priority = 0)
    {
        public Parameter Parameter { get; } = parameter;
        public ModifierType ModifierType { get; } = type;
        public float ValuePerPoint { get; } = valuePerPoint;
        public int Priority { get; } = priority;
    }
}
