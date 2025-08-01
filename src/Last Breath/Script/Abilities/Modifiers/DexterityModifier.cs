namespace LastBreath.Script.Abilities.Modifiers
{
    using Contracts.Enums;

    public class DexterityModifier : ModifierBase
    {
        public DexterityModifier( ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.Dexterity,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
