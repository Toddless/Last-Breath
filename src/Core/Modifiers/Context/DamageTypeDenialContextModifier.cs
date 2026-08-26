namespace Core.Modifiers.Context
{
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Outgoing-damage rule: the owner deals no damage of one type at all, optionally only from one
    /// <see cref="DamageCause"/> ("attacks deal no physical damage"). The component is emptied rather than
    /// removed, so everything downstream still sees the type the hit was made of.
    /// <para>Runs at <see cref="ContextModifierPriority.Denial"/>, after the conversions at
    /// <see cref="ContextModifierPriority.Absolute"/>: a share another rule has already carried into a
    /// different type is that type's by then and survives, and only what is still of the denied type is
    /// lost.</para></summary>
    public class DamageTypeDenialContextModifier(IFightable owner, DamageType type, DamageCause? cause = null)
        : ContextModifier(priority: ContextModifierPriority.Denial, id: "Context_Modifier_Damage_Type_Denial"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (cause != null && context.Cause != cause) return;
            if (!context.DamageComponents.ContainsKey(type)) return;

            context.Set(type, 0f);
        }
    }
}
