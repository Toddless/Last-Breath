namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class CurrentHealthModifier(ModifierType type, float value, int priority = 0)
        : ModifierBase(parameter: Parameter.CurrentHealth,
            type,
            value,
            priority)
    {
    }
}
