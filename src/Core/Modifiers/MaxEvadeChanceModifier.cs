namespace Core.Modifiers
{
    using Core.Enums;

    public class MaxEvadeChanceModifier : ModifierBase
    {
        public MaxEvadeChanceModifier(ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.MaxEvadeChance,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
