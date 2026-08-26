namespace Core.Modifiers.Context
{
    using System;
    using Core.Context;
    using Enums;

    /// <summary>Immunity to hard control on its bearer: an effect carrying any status of the mask is refused
    /// outright instead of being shortened, the categorical end of the scale
    /// <see cref="ControlResistanceModifier"/> walks in tiers.
    /// <para>The mask is asked for at the moment of application rather than kept: which statuses count as
    /// hard control is a rules file's answer, and the bearer is covered by whatever it says now.</para></summary>
    public class ControlImmunityModifier(Func<StatusEffects> hardControl)
        : ContextModifier(ContextModifierPriority.Late, id: "Context_Modifier_Control_Immunity"), IIncomingEffectModifier
    {
        public void Apply(IIncomingEffectContext context)
        {
            if ((context.Effect.Status & hardControl()) == 0) return;

            context.Rejected = true;
        }
    }
}
