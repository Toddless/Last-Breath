namespace Battle.Source.Abilities.Overload
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>L2 upgrade: every burned mana point converts into more damage.</summary>
    public class OvUpgradeDamagePerMana(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<Overload>(id, tags, tier)
    {
        private const string DecoratorId = "Ability_Parameter_Decorator_Ov_Damage_Per_Mana";

        public override void ApplyUpgrade(Overload ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                Overload.Parameters.DamagePerMana, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(Overload ability) =>
            ability.RemoveParameterDecorator(DecoratorId, Overload.Parameters.DamagePerMana);

        public override IAbilityUpgrade Copy() => new OvUpgradeDamagePerMana(Id, Tags, Tier, amount);
    }
}
