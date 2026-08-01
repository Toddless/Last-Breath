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
    /// check prices the cast the owner will actually pay. <see cref="StacksChanged"/> fires on
    /// every actual change, which is what carries the count out to the player.
    /// </summary>
    public class ExhaustionModifier(ExhaustionRules rules)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Exhaustion"), IAbilityActivationModifier
    {
        public int Stacks
        {
            get;
            private set
            {
                if (field == value) return;
                field = value;
                StacksChanged?.Invoke(field);
            }
        }

        /// <summary>The fraction the current stacks add to every ability cost (0.75 = +75%).
        /// One formula for both readers: <see cref="Apply"/> charges it, the UI shows it.</summary>
        public float Surcharge => Stacks * rules.CostIncreasePerStack;

        /// <summary>Raised with the new count whenever it actually changes; an unchanged count is silent.</summary>
        public event Action<int>? StacksChanged;

        public void Apply(IAbilityActivationContext context)
        {
            if (Stacks == 0) return;
            context.Cost *= 1f + Surcharge;
        }

        public void OnAbilityActivated() => Stacks++;

        public void DecayTick() => Stacks = Math.Max(0, Stacks - rules.DecayPerTurn);

        public void Reset() => Stacks = 0;
    }
}
