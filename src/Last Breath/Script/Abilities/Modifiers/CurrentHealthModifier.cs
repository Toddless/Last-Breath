namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

    public class CurrentHealthModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.CurrentHealth,
            type,
            value,
            source,
            priority)
    {
    }
}
