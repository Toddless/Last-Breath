namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class ResourceModifier : ModifierBase
    {
        public ResourceModifier(ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.Resource,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
