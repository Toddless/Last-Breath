namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

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
