namespace Core.Modifiers.Context
{
    using Battle;
    using Enums;

    public class FirstAttackCritContextModifier(float criticalDamageBonus)
        : ContextModifier(priority: ContextModifierPriority.Normal, id:"Modifier_First_Attack_Crit"),IAttackModifier
    {
        public void Apply(IAttackContext context)
        {
            if (context.IsFirst) context.RawCriticalDamage += criticalDamageBonus;
        }
    }
}
