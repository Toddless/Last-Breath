namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class DamageModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.StrikeDamage,
            type,
            value,
            source,
            priority)
    {
    }
}
