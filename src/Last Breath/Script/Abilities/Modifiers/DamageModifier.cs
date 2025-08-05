namespace LastBreath.Script.Abilities.Modifiers
{
    using Core.Enums;

    public class DamageModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.Damage,
            type,
            value,
            source,
            priority)
    {
    }
}
