namespace Core.Modifiers.Context
{
    using System;
    using Battle;
    using Battle.Abilities;
    using Enums;

    /// <summary>
    /// Exhaustion: every ability activated this turn stacks a cost surcharge on ALL abilities —
    /// burst is allowed, spam is cut. One instance per combatant. The bearer's wiring feeds it:
    /// a stack per <c>AbilityActivatedEvent</c> (a multicast rolls stages within ONE activation —
    /// one stack), <see cref="DecayTick"/> on the bearer's own turn end, <see cref="Reset"/> on
    /// battle end. The surcharge is deterministic, so previews see it too — the availability
    /// check prices the cast the owner will actually pay.
    /// </summary>
    public class ExhaustionModifier(ExhaustionRules rules)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Exhaustion"), IAbilityActivationModifier
    {
        public int Stacks { get; private set; }

        public void Apply(IAbilityActivationContext context)
        {
            if (Stacks == 0) return;
            context.Cost *= 1f + Stacks * rules.CostIncreasePerStack;
        }

        public void OnAbilityActivated() => Stacks++;

        public void DecayTick() => Stacks = Math.Max(0, Stacks - rules.DecayPerTurn);

        public void Reset() => Stacks = 0;
    }
}
