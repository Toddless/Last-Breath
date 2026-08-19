namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Buys both damage coefficients of the cast and charges a share more for it. The coefficients are
    /// figures of their own — a scale is already a fraction, and adding to it is what every scale record
    /// in the book does — while the surcharge is a share, because a flat one would rewrite a cheap
    /// ability and go unnoticed on an expensive one.
    /// </summary>
    public class AbilityAugmentRaiseScalesAddCost(
        string id, string[] tags, int tier, float weaponScale, float spellScale, float costShare)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        private string WeaponDecoratorId => $"Ability_Parameter_Decorator_{Id}_Weapon";
        private string SpellDecoratorId => $"Ability_Parameter_Decorator_{Id}_Spell";
        private string CostDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cost";

        public override void ApplyUpgrade(Ability ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.WeaponDamageScale, Priority.Weak, OperationType.Add, weaponScale, WeaponDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.SpellDamageScale, Priority.Weak, OperationType.Add, spellScale, SpellDecoratorId, Id));
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.CostValue, OperationType.Add, costShare, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            ability.RemoveParameterDecorator(WeaponDecoratorId, AbilityParameter.WeaponDamageScale);
            ability.RemoveParameterDecorator(SpellDecoratorId, AbilityParameter.SpellDamageScale);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAbilityAugment Copy() =>
            new AbilityAugmentRaiseScalesAddCost(Id, Tags, Tier, weaponScale, spellScale, costShare);
    }
}
