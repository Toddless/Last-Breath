namespace Battle.Source.Abilities.HeadButt
{
    using Core.Battle.Abilities;
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    /// <summary>L3 upgrade: the head butt performs additional lunges (two in total).</summary>
    public class HbUpgradeAdditionalLunges(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<HeadButt>(id, tags, tier)
    {
        private const string DecoratorId = "Ability_Parameter_Decorator_Additional_Lunges";

        public override void ApplyUpgrade(HeadButt ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<HeadButt.Parameters>(
                HeadButt.Parameters.Attacks, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(HeadButt ability) =>
            ability.RemoveParameterDecorator(DecoratorId, HeadButt.Parameters.Attacks);

        public override IAbilityUpgrade Copy() => new HbUpgradeAdditionalLunges(Id, Tags, Tier, amount);
    }
}
