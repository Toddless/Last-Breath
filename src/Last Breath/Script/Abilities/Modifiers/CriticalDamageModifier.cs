namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class CriticalDamageModifier(ModifierType type, float value, int priority = 0)
        : ModifierBase(parameter: Parameter.CriticalStrikeDamage,
            type,
            value,
            priority)
    {
    }
}
