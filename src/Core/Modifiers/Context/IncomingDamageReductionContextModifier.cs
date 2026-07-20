namespace Core.Modifiers.Context
{
    using System.Linq;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Reduces every damage component of hits the owner TAKES by <c>reduce</c> (0.25 = −25%),
    /// optionally only from one <see cref="DamageCause"/> ("−15% damage from abilities"). The owner gate is
    /// not optional: the handler also runs on damage the owner DEALS, and without it a defensive line would
    /// quietly cut the wearer's own output.</summary>
    public class IncomingDamageReductionContextModifier(IFightable owner, float reduce, DamageCause? cause = null)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Incoming_Damage_Reduction"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (context.Source.IsSame(owner.InstanceId)) return;
            if (cause != null && context.Cause != cause) return;

            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, damage * (1 - reduce));
        }
    }
}
