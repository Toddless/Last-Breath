namespace Core.Modifiers.Context
{
    using Battle;
    using Enums;

    public class UnevadableAttackContextModifier()
        : ContextModifier(priority: Priority.Absolute, id: "Modifier_Unevadable_Attack"), IAttackModifier
    {
        public void Apply(IAttackContext context) => context.IsUnevadable = true;
    }
}
