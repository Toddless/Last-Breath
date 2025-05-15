namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public class AllAtributeModifier : ModifierBase
    {
        public AllAtributeModifier( ModifierType type, float value, object source, int priority = 0)
            : base(parameter: Parameter.AllAttribute,
                  type,
                  value,
                  source,
                  priority)
        {
        }
    }
}
