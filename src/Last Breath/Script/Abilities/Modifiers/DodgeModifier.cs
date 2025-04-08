namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class DodgeModifier(ModifierType type, float value, int priority = 0)
        : ModifierBase(parameter: Parameter.Dodge,
            type,
            value,
            priority)
    {
    }
}
