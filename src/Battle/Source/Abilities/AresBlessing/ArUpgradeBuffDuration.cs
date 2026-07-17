namespace Battle.Source.Abilities.AresBlessing
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>L1 upgrade: the blessing lasts longer.</summary>
    public class ArUpgradeBuffDuration(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<AresBlessing>(id, tags, tier)
    {
        private const string DecoratorId = "Ability_Parameter_Decorator_Ar_Duration";

        public override void ApplyUpgrade(AresBlessing ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AresBlessing.Parameters.Duration, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(AresBlessing ability) =>
            ability.RemoveParameterDecorator(DecoratorId, AresBlessing.Parameters.Duration);

        public override IAbilityUpgrade Copy() => new ArUpgradeBuffDuration(Id, Tags, Tier, amount);
    }
}
