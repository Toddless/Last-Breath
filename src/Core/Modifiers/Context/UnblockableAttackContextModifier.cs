namespace Core.Modifiers.Context
{
    using Battle;
    using Enums;

    public class UnblockableAttackContextModifier()
        : ContextModifier(priority: ContextModifierPriority.Absolute, id:"Modifier_Unblockable_Attack"),IAttackModifier
    {
        public void Apply(IAttackContext context) => context.IsUnblockable = true;
    }
}
