namespace LastBreath.Script.Abilities.Modifiers
{
    using Core.Enums;

    public class MaxHealthModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.MaxHealth,
            type,
            value,
            source,
            priority)
    {
    }
}
