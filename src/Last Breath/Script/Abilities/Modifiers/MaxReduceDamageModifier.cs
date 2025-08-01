namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

    public class MaxReduceDamageModifier : ModifierBase
    {
        public MaxReduceDamageModifier(ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.MaxReduceDamage,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
