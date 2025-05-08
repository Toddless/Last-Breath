namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class AdditionalHitModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.AdditionalAttackChance,
            type,
            value,
            source,
            priority)
    {
    }
}
