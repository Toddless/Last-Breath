namespace Core.Modifiers.Context
{
    using Battle;
    using Core.Context;
    using Enums;

    public class LeechOnCritAttackContextModifier(float leechPercent)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Attack_Modifier_Leech_On_Crit"), IAttackModifier
    {
        public void Apply(IAttackContext context)
        {
            if (!context.IsCritical) return;
            var attacker = context.Attacker;
            attacker.Heal(new HealContext(attacker, attacker) { Amount = context.FinalDamage.Total * leechPercent, Cause = RecoveryCause.Leech });
        }
    }
}
