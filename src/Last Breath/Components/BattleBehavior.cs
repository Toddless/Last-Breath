namespace Playground.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Components.Interfaces;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Scenes;

    /// <summary>
    /// Determine enemies battle behaviors depending on conditions
    /// </summary>
    public class BattleBehavior
    {
        private List<IAbilityDecision>? _abilitiesDecisions;
        private List<ICondition>? _conditions;
        private IBattleContext? _battleContext;

        public BattleBehavior()
        {
        }

        public void SetDependencies(IBattleContext context, IConditionsFactory conditions)
        {
            _battleContext = context;
            _conditions = conditions.SetNewConditions(_battleContext);
            SetAbilitiesDecisions(_battleContext.Self.Abilities!, _conditions!);
        }

        public IAbility? MakeDecision()
        {
            // update decisions bevor taking an ability
            UpdateAbilitiesDecisions();
            return _abilitiesDecisions?.OrderByDescending(x => x.Priority).FirstOrDefault(x => x.Ability.Cooldown == 4)?.Ability;
        }

        public void BattleEnds()
        {
            _battleContext = null;
            _abilitiesDecisions = null;
            _conditions = null;
        }

        private float EvaluateAbility(IAbility ability, List<ICondition> conditions)
        {
            float biggestPriority = 0;
            foreach (var condition in conditions)
            {
                float priority = condition.Weight * AbilityMatchConditionNeeds(ability, condition);
                if (priority > biggestPriority)
                    biggestPriority = priority;
            }
            return biggestPriority;
        }

        private float AbilityMatchConditionNeeds(IAbility ability, ICondition condition)
        {
            float priorityModifier = 1;
            foreach (IEffect effect in ability.Effects)
            {
                if (effect.EffectType == condition.EffectNeeded)
                    priorityModifier += 0.1f;
                if (condition.CheckCondition.Invoke())
                    priorityModifier += 0.5f;
            }

            return priorityModifier;
        }

        private void SetAbilitiesDecisions(List<IAbility> abilities, List<ICondition> conditions)
        {
            _abilitiesDecisions = [];
            foreach (var ability in abilities)
            {
                _abilitiesDecisions?.Add(new AbilityDecision(ability, EvaluateAbility(ability, conditions)));
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
