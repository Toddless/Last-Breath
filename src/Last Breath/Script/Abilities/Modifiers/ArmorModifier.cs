namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class ArmorModifier(ModifierType type, float value, int priority = 0)
        : ModifierBase(parameter: Parameter.Armor,
            type,
            value,
            priority)
    {
    }
}
