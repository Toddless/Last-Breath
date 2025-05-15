namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

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
