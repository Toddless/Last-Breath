namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

    public class AdditionalHitModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.AdditionalHitChance,
            type,
            value,
            source,
            priority)
    {
    }
}
