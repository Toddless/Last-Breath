namespace Core.Modifiers.Context
{
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Outgoing-damage mutator: attacks and ability hits dealt by <paramref name="owner"/>
    /// bypass the target's barrier. DoT ticks stay honest — they belong to their effects.</summary>
    public class BarrierBypassContextModifier(IFightable owner)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Barrier_Bypass"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (context.Cause is not (DamageCause.Attack or DamageCause.Ability)) return;
            context.IgnoreBarrier = true;
        }
    }
}
