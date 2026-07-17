namespace Core.Modifiers.Context
{
    using Core.Context;
    using Enums;

    /// <summary>
    /// Changes recovery value by amount.
    /// </summary>
    /// <param name="amount"> 1.3 for increase, 0.9 for reduce</param>
    public class HealthRecoveryContextModifier(float amount)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Mana_Recovery"), IHealModifier
    {
        public void Apply(IHealContext context) => context.Amount *= amount;
    }
}
