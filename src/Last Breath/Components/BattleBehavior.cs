namespace Playground.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Scenes;

    /// <summary>
    /// Determine enemies battle behaviors depending on conditions
    /// </summary>
    public class BattleBehavior
    {
        private List<AbilityDecision>? _abilitiesDecisions;
        private List<Condition>? _conditions;
        private IBattleContext? _battleContext;

        public BattleBehavior()
        {
            _abilitiesDecisions = [];
            // i need to define somewhere set of conditions.
            // conditions have different priorities depends on battle behavior (defense behavior, aggressive behavior etc.)
        }

        public void SetDependencies()
        {
            _conditions = ConditionsFactory.SetNewConditions();
        }

        public IAbility? MakeDecision()
        {
            UpdateAbilitiesDecisions();
            return _abilitiesDecisions?.OrderByDescending(x => x.Priority).FirstOrDefault(x => x.Ability.Cooldown == 4)?.Ability;
        }

        public void GatherInfo(Player player, IBattleContext context)
        {
            _battleContext = context;
            SetAbilitiesDecisions(_battleContext.Self.Abilities!, _conditions!);
        }

        public void BattleEnds()
        {
            _battleContext = null;
        }

        private float EvaluateAbility(IAbility ability, List<Condition> conditions)
        {
            float priority = 0;

            foreach (Condition condition in conditions)
            {
                if (condition.CheckCondition.Invoke())
                {
                    priority += condition.Weight * AbilityMatchConditionNeeds(ability, condition);
                }
            }
            return priority;
        }

        private float AbilityMatchConditionNeeds(IAbility ability, Condition condition)
        {
            float priorityModifier = 1;
            foreach (IEffect effect in ability.Effects)
            {
                // Only effect modifier count, i need some other info too (damage, heal amount, defense etc.)
                if (effect.EffectType == condition.EffectNeeded)
                    priorityModifier += effect.Modifier;
            }

            return priorityModifier;
        }

        private void SetAbilitiesDecisions(List<IAbility> abilities, List<Condition> conditions)
        {
            foreach (var ability in abilities)
            {
                _abilitiesDecisions?.Add(new(ability, EvaluateAbility(ability, conditions)));
            }
        }

        private void UpdateAbilitiesDecisions()
        {
            foreach (var item in _abilitiesDecisions!)
            {
                item.Priority = UpdateAbilityPriority(item.Ability);
            }
        }

        private float UpdateAbilityPriority(IAbility ability)
        {
            return EvaluateAbility(ability, _conditions!);
        }
    }
}
