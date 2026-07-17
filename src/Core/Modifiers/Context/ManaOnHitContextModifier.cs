namespace Core.Modifiers.Context
{
    using System;
    using Battle;
    using Core.Context;
    using Enums;

    public class ManaOnHitContextModifier(Func<float> amount)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Mana_On_Hit"), IAttackModifier
    {
        public ManaOnHitContextModifier(float amount)
            : this(() => amount)
        {

        }
        public void Apply(IAttackContext context)
        {
            if (context.Result is not AttackResults.Succeed) return;
            context.Attacker.RestoreMana(new ManaRecoveryContext(context.Attacker, context.Attacker) { Amount = amount() });
        }
    }
}
