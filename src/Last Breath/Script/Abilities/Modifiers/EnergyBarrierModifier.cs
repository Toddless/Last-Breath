namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

    public class EnergyBarrierModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.EnergyBarrier,
            type,
            value,
            source,
            priority)
    {
    }
}
