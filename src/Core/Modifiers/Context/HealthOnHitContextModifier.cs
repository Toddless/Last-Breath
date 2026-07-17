namespace Core.Modifiers.Context
{
    using System;
    using Enums;
    using Battle;
    using Core.Context;

    public class HealthOnHitContextModifier(Func<float> amount)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Health_On_Hit"), IAttackModifier
    {
        public HealthOnHitContextModifier(float amount)
            : this(() => amount)
        {
        }

        public void Apply(IAttackContext context)
        {
            if (context.Result is not AttackResults.Succeed) return;
            context.Attacker.Heal(new HealContext(context.Attacker, context.Attacker) { Amount = amount(), Cause = HealCause.Direct });
        }
    }
}
