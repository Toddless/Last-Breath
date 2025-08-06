namespace Core.Modifiers
{
    using Core.Enums;

    public class ArmorModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.Armor,
            type,
            value,
            source,
            priority)
    {
    }
}
