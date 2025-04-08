namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class AdditionalHitModifier(ModifierType type, float value, int priority = 0)
        : ModifierBase(parameter: Parameter.AdditionalStrikeChance,
            type,
            value,
            priority)
    {
    }
}
