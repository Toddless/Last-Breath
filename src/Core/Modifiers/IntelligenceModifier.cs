namespace Core.Modifiers
{
    using Core.Enums;

    public class IntelligenceModifier : ModifierBase
    {
        public IntelligenceModifier(ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.Intelligence,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
