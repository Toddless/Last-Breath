namespace Playground.Components
{
    using System.Collections.Generic;
    using Playground.Components.Interfaces;
    using Playground.Script.ScenesHandlers;

    // TODO: Rework
    public class BattleBehavior
    {
        private List<IAbilityDecision>? _abilitiesDecisions;
        private IBattleContext? _battleContext;

        //public void SetDependencies(IBattleContext context)
        //{
        //    _battleContext = context;
        //    SetAbilitiesDecisions(_battleContext.Self.Abilities!);
        //}

        //public IAbility? MakeDecision()
        //{
        //    UpdateAbilitiesDecisions();
        //    return _abilitiesDecisions?.OrderByDescending(x => x.Priority).FirstOrDefault(x => x.Ability.Cooldown == 4)?.Ability;
        //}

        //public void BattleEnds()
        //{
        //    _battleContext = null;
        //    _abilitiesDecisions = null;
        //}

        //private float EvaluateAbility(IAbility ability)
        //{
        //    float biggestPriority = 0;

        //    return biggestPriority;
        //}

        //private float AbilityMatchConditionNeeds(IAbility ability)
        //{
        //    float priorityModifier = 1;
        //    foreach (IEffect effect in ability.Effects)
        //    {
        //      //  if (effect.EffectType == condition.EffectNeeded)
        //            priorityModifier += 0.1f;
        //      //if (condition.CheckCondition.Invoke())
        //            priorityModifier += 0.5f;
        //    }

        //    return priorityModifier;
        //}

        //private void SetAbilitiesDecisions(List<IAbility> abilities)
        //{
        //    _abilitiesDecisions = [];
        //    foreach (var ability in abilities)
        //    {
        //        _abilitiesDecisions?.Add(new AbilityDecision(ability, EvaluateAbility(ability)));
        //    }
        //}

        //private void UpdateAbilitiesDecisions()
        //{
        //    foreach (var item in _abilitiesDecisions!)
        //    {
        //        item.Priority = UpdateAbilityPriority(item.Ability);
        //    }
        //}

        //private float UpdateAbilityPriority(IAbility ability)
        //{
        //    return EvaluateAbility(ability);
        //}
    }
}
