namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Buys both damage coefficients of the cast and charges a share more for it. The coefficients are
    /// figures of their own — a scale is already a fraction, and adding to it is what every scale record
    /// in the book does — while the surcharge is a share, because a flat one would rewrite a cheap
    /// ability and go unnoticed on an expensive one.
    ///
    /// Both points are bought for the CAST, so they go on every figure it deals
    /// (<see cref="AbilityParameterSet.Family"/>): a delivery of two strikes or of several stages spells a
    /// pair of coefficients per figure, and a record standing on the book's pair alone would raise the
    /// first touch and charge for all of them.
    /// </summary>
    public class AugmentRaiseScalesAddCost(
        string id, string[] tags, int tier, float weaponScale, float spellScale, float costShare)
        : Augment<Ability>(id, tags, tier)
    {
        private string CostDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cost";

        public override void ApplyUpgrade(Ability ability)
        {
            RaiseScales(ability, AbilityParameter.WeaponDamageScale, weaponScale);
            RaiseScales(ability, AbilityParameter.SpellDamageScale, spellScale);
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.CostValue, OperationType.Add, costShare, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            DropScales(ability, AbilityParameter.WeaponDamageScale);
            DropScales(ability, AbilityParameter.SpellDamageScale);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAugment Copy() =>
            new AugmentRaiseScalesAddCost(Id, Tags, Tier, weaponScale, spellScale, costShare);

        private void RaiseScales(Ability ability, string scale, float points)
        {
            foreach (string parameter in ability.Family(scale))
                ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                    parameter, Priority.Weak, OperationType.Add, points, DecoratorId(parameter), Id));
        }

        private void DropScales(Ability ability, string scale)
        {
            foreach (string parameter in ability.Family(scale))
                ability.RemoveParameterDecorator(DecoratorId(parameter), parameter);
        }
    }
}
