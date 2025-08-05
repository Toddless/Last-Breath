namespace LastBreath.Script.Abilities.Modifiers
{
    using Core.Enums;

    public class CriticalChanceModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.CriticalChance,
            type,
            value,
            source,
            priority)
    {
    }
}
