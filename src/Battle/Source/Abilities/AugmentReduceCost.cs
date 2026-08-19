namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Takes a share off what the ability costs to cast. A share and not a number of points, because
    /// one augment now serves the whole book: what the abilities it goes on charge runs from nothing at
    /// all to five hundred, and a single flat discount would be a near-free cast at the cheap end and
    /// noise at the expensive one. The share is measured against the ability's own base price and
    /// rounded there — see <see cref="AbilityParameterShare"/> — so what the augment takes off is
    /// the same number whatever else is worn beside it.
    /// </summary>
    public class AugmentReduceCost(string id, string[] tags, int tier, float costShare)
        : Augment<Ability>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_{Id}";

        public override void ApplyUpgrade(Ability ability) =>
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.CostValue,
                OperationType.Subtract,
                costShare,
                DecoratorId,
                Id));

        public override void RemoveUpgrade(Ability ability) =>
            ability.RemoveParameterDecorator(DecoratorId, AbilityParameter.CostValue);

        public override IAugment Copy() => new AugmentReduceCost(Id, Tags, Tier, costShare);
    }
}
