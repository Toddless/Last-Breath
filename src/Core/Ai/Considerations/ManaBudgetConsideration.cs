namespace Core.Ai.Considerations
{
    using System;
    using System.Collections.Generic;
    using Battle.Abilities;
    using Entity;
    using Enums;

    /// <summary>
    /// Discourages burning most of the mana pool on one cast: the tighter the cast sits
    /// against the current mana, the stronger the dampening (down to <see cref="MinFactor"/>).
    /// Non-mana costs (health, barrier) pass through — Sacrifice-style abilities price themselves.
    /// </summary>
    public class ManaBudgetConsideration : ICombatConsideration
    {
        private const float MinFactor = 0.5f;

        public float Evaluate(CombatBlackboard board, IAbility ability, AbilityRole role, IReadOnlyList<IFightable> targets)
        {
            if (ability.CostType != Costs.Mana || ability.CostValue <= 0) return 1f;

            float mana = Math.Max(board.Self.CurrentMana, 1f);
            float bite = Math.Clamp(ability.CostValue / mana, 0f, 1f);
            return 1f - (1f - MinFactor) * bite;
        }
    }
}
