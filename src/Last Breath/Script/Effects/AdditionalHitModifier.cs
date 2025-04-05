namespace Playground.Script.Effects
{
    using Playground.Script.Enums;

    public class AdditionalHitModifier(Parameter parameter, ModifierType type, float value, int priority = 0) : ModifierBase(parameter, type, value, priority)
    {
    }
}
