namespace Core.Modifiers.Context
{
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Outgoing-damage switch: the owner's hits skip the target's elemental resistances, optionally
    /// only from one <see cref="DamageCause"/> ("attacks ignore elemental resistances"). Mitigation reads the
    /// flag, so nothing here touches the numbers.</summary>
    public class IgnoreResistancesContextModifier(IFightable owner, DamageCause? cause = null)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Ignore_Resistances"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (cause != null && context.Cause != cause) return;

            context.IgnoreResistances = true;
        }
    }
}
