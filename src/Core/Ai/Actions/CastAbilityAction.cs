namespace Core.Ai.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Considerations;
    using Interfaces.Abilities;
    using Interfaces.Entity;
    using Targeting;

    /// <summary>
    /// Casting one ability at the best targets its own targeting strategy allows.
    /// Score = profile weight × archetype bias × considerations × noise; targets picked
    /// during scoring are reused by Execute. Charged abilities take MaxAffordableStage × Greed.
    /// </summary>
    public class CastAbilityAction(IAbility ability, AbilityBehavior behavior, IReadOnlyList<ICombatConsideration> considerations) : ICombatAction
    {
        private IReadOnlyList<IFightable> _plannedTargets = [];

        public IAbility Ability { get; } = ability;

        public bool CanExecute(CombatBlackboard board)
        {
            if (!Ability.CanActivate()) return false;
            _plannedTargets = PlanTargets(board);
            return _plannedTargets.Count > 0 || !Ability.Targeting.RequiresManualSelection;
        }

        public float Score(CombatBlackboard board)
        {
            var profile = board.Profile;
            float score = behavior.Weight * ArchetypeBias(profile);

            if (profile.Intellect != AiIntellect.Simple)
            {
                foreach (var consideration in considerations)
                    score *= consideration.Evaluate(board, Ability, behavior.Role, _plannedTargets);
            }

            return score * NoiseFactor(board);
        }

        public Task ExecuteAsync(CombatBlackboard board)
        {
            ChargeIfCharged(board.Profile);
            board.CastsThisTurn++;
            return board.Environment.CastAbilityAsync(board.Self, Ability, _plannedTargets.ToList());
        }

        /// <summary>
        /// Manual strategies (the player would click) get the ranked top-N of the valid set;
        /// automatic ones (self/random) resolve at execution — an empty plan is legitimate for them.
        /// </summary>
        private IReadOnlyList<IFightable> PlanTargets(CombatBlackboard board)
        {
            var strategy = Ability.Targeting;
            if (!strategy.RequiresManualSelection)
                return strategy.ResolveAutomatic(board.Self, board.Environment.Field);

            var valid = strategy.GetValidTargets(board.Self, board.Environment.Field);
            var ranked = TargetRanker.Rank(valid, behavior.Role);
            return ranked.Take(Math.Max(1, strategy.MaxTargets)).ToList();
        }

        private float ArchetypeBias(BehaviorProfile profile) => behavior.Role switch
        {
            AbilityRole.Damage or AbilityRole.Debuff => profile.Aggression,
            AbilityRole.Heal or AbilityRole.Buff or AbilityRole.Control => profile.Caution,
            _ => 1f
        };

        private static float NoiseFactor(CombatBlackboard board)
        {
            float temperature = board.Profile.Temperature;
            if (board.Profile.Intellect == AiIntellect.Mastermind) temperature *= 0.5f;
            return temperature <= 0 ? 1f : 1f + board.Rnd.RandFloatRange(-temperature, temperature);
        }

        private void ChargeIfCharged(BehaviorProfile profile)
        {
            if (Ability is not IChargedAbility charged) return;
            int stage = (int)MathF.Round(charged.MaxAffordableStage * profile.Greed);
            charged.PendingStage = Math.Clamp(stage, 1, Math.Max(1, charged.MaxAffordableStage));
        }
    }
}
