namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

    public class ResourceRecoveryModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.ResourceRecovery,
            type,
            value,
            source,
            priority)
    {
    }
}
