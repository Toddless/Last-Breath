namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class EvadeModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.Evade,
            type,
            value,
            source,
            priority)
    {
    }
}
