namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class SuppressModifier : ModifierBase
    {
        public SuppressModifier(ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.Suppress,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
