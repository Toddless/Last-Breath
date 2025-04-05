namespace PlaygroundTest.ComponentTesting
{
    using Playground.Script.Effects;
    using Playground.Script.Enums;

    public class ModifierTest(Parameter parameter, ModifierType type, float value, int priority = 0) : ModifierBase(parameter, type, value, priority)
    {
    }
}
