namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class StrikeDamageModifier(ModifierType type, float value, int priority = 0)
        : ModifierBase(parameter: Parameter.StrikeDamage,
            type,
            value,
            priority)
    {
    }
}
