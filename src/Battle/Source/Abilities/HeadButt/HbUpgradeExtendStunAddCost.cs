namespace Battle.Source.Abilities.HeadButt
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    /// <summary>L2 upgrade: the stun lasts longer at the price of an increased resource cost.</summary>
    public class HbUpgradeExtendStunAddCost(string id, string[] tags, int tier, float stunDuration, float additionalCost)
        : AbilityUpgrade<HeadButt>(id, tags, tier)
    {
        private const string StunDecoratorId = "Ability_Parameter_Decorator_Extend_Stun";
        private const string CostDecoratorId = "Ability_Parameter_Decorator_Stun_Additional_Cost";

        public override void ApplyUpgrade(HeadButt ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<HeadButt.Parameters>(
                HeadButt.Parameters.StunDuration, Priority.Weak, OperationType.Add, stunDuration, StunDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.CostValue, Priority.Weak, OperationType.Add, additionalCost, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(HeadButt ability)
        {
            ability.RemoveParameterDecorator(StunDecoratorId, HeadButt.Parameters.StunDuration);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAbilityUpgrade Copy() => new HbUpgradeExtendStunAddCost(Id, Tags, Tier, stunDuration, additionalCost);
    }
}
