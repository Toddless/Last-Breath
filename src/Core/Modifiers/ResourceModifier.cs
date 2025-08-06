namespace Core.Modifiers
{
    using Core.Enums;

    public class ResourceModifier : ModifierBase
    {
        public ResourceModifier(ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.ResourceMax,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
