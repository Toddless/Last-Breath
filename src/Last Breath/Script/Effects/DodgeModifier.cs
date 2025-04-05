namespace Playground.Script.Effects
{
    using Playground.Script.Enums;

    public class DodgeModifier(Parameter parameter, ModifierType type, float value, int priority = 0) : ModifierBase(parameter, type, value, priority)
    {
    }
}
