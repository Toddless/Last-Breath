namespace Battle.Source.Abilities.Overload
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Decorators;

    /// <summary>L1 upgrade: burns a bigger share of the target's mana at the price of an increased cost.</summary>
    public class OvUpgradeBurnAddCost(string id, string[] tags, int tier, float burnPercent, float additionalCost)
        : AbilityUpgrade<Overload>(id, tags, tier)
    {
        private const string BurnDecoratorId = "Ability_Parameter_Decorator_Ov_Burn";
        private const string CostDecoratorId = "Ability_Parameter_Decorator_Ov_Burn_Cost";

        public override void ApplyUpgrade(Overload ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<Overload.Parameters>(
                Overload.Parameters.ManaBurnPercent, Priority.Weak, OperationType.Override, burnPercent, BurnDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.CostValue, Priority.Weak, OperationType.Add, additionalCost, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(Overload ability)
        {
            ability.RemoveParameterDecorator(BurnDecoratorId, Overload.Parameters.ManaBurnPercent);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAbilityUpgrade Copy() => new OvUpgradeBurnAddCost(Id, Tags, Tier, burnPercent, additionalCost);
    }
}
