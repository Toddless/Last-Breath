namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

    public class StrengthModifier : ModifierBase
    {
        public StrengthModifier( ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.Strength,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
