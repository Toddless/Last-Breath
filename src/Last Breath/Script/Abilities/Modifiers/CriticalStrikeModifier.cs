namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class CriticalStrikeModifier(ModifierType type, float value, int priority = 0)
        : ModifierBase(parameter: Parameter.CriticalStrikeChance,
            type,
            value,
            priority)
    {
    }
}
