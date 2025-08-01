namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

    public class MovespeedModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.Movespeed,
            type,
            value,
            source,
            priority)
    {
    }
}
