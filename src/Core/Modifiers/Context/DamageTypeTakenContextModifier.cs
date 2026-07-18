namespace Core.Modifiers.Context
{
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>
    /// Amplifies the <paramref name="type"/> component of hits TAKEN by <paramref name="owner"/> by
    /// <c>amp</c> (0.15 = +15%). The handler also runs when the owner deals damage, hence the source gate.
    /// </summary>
    public class DamageTypeTakenContextModifier(IFightable owner, DamageType type, float amp)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Damage_Type_Taken"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (context.Source.IsSame(owner.InstanceId)) return;
            if (!context.DamageComponents.TryGetValue(type, out float damage) || damage <= 0) return;
            context.Set(type, damage * (1 + amp));
        }
    }
}
