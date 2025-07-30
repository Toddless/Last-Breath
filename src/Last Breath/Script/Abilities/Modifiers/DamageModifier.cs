namespace LastBreath.Script.Abilities.Modifiers
{
    using LastBreath.Script.Enums;

    public class DamageModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.Damage,
            type,
            value,
            source,
            priority)
    {
    }
}
