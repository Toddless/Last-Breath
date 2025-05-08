namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class EnergyBarrierModifier(ModifierType type, float value, object source, int priority = 0)
        : ModifierBase(parameter: Parameter.EnergyBarrier,
            type,
            value,
            source,
            priority)
    {
    }
}
