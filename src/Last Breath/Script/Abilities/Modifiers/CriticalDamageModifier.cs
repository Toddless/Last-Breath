namespace LastBreath.Script.Abilities.Modifiers
{
    using LastBreath.Script.Enums;

    public class CriticalDamageModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.CriticalDamage,
            type,
            value,
            source,
            priority)
    {
    }
}
