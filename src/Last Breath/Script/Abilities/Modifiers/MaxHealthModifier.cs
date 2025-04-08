namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class MaxHealthModifier(ModifierType type, float value, int priority = 0)
        : ModifierBase(parameter: Parameter.MaxHealth,
            type,
            value,
            priority)
    {
    }
}
