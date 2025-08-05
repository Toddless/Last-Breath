namespace LastBreath.Script.Abilities.Modifiers
{
    using Core.Enums;

    public class SpellDamageModfier : ModifierBase
    {
        public SpellDamageModfier(ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.SpellDamage,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
