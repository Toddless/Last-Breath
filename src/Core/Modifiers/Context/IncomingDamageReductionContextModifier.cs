namespace Core.Modifiers.Context
{
    using System;
    using System.Linq;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Reduces every damage component of hits the owner TAKES by <c>reduce</c> (0.25 = −25%),
    /// optionally only from one <see cref="DamageCause"/> ("−15% damage from abilities"). The gate is on the
    /// RECEIVER and not optional: the handler also runs on damage the owner DEALS, and without it a defensive
    /// line would quietly cut the wearer's own output. Damage the owner inflicts on himself passes it — such a
    /// hit is still one he takes, and its cause is the mechanism that dealt it (a self-burn is Effect damage).
    /// The reduction is read lazily, so a source whose value changes (item upgrade) is picked up without
    /// re-attach — sharpening a WORN piece runs no re-equip. Read once per hit so every component of the
    /// same hit shares one number.</summary>
    public class IncomingDamageReductionContextModifier(IFightable owner, Func<float> reduce, DamageCause? cause = null)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Incoming_Damage_Reduction"), IDamageModifier
    {
        /// <summary>For sources whose reduction cannot change while the modifier lives (a buff instance).</summary>
        public IncomingDamageReductionContextModifier(IFightable owner, float reduce, DamageCause? cause = null)
            : this(owner, () => reduce, cause)
        {
        }

        public void Apply(IDamageContext context)
        {
            if (!context.Target.IsSame(owner.InstanceId)) return;
            if (cause != null && context.Cause != cause) return;

            float reduction = reduce();
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, damage * (1 - reduction));
        }
    }
}
