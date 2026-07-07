namespace Battle.Source.Abilities.AresBlessing
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    /// <summary>L2 upgrades: extra health and/or recovery bonus (either one may be zero).</summary>
    public class ArUpgradeBlessingBonus(string id, string[] tags, int tier, float healthBonus, float recoveryBonus)
        : AbilityUpgrade<AresBlessing>(id, tags, tier)
    {
        private const string HealthDecoratorId = "Ability_Parameter_Decorator_Ar_Health_Bonus";
        private const string RecoveryDecoratorId = "Ability_Parameter_Decorator_Ar_Recovery_Bonus";

        public override void ApplyUpgrade(AresBlessing ability)
        {
            if (healthBonus > 0)
                ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AresBlessing.Parameters>(
                    AresBlessing.Parameters.HealthBonus, Priority.Weak, OperationType.Add, healthBonus, HealthDecoratorId, Id));
            if (recoveryBonus > 0)
                ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AresBlessing.Parameters>(
                    AresBlessing.Parameters.RecoveryBonus, Priority.Weak, OperationType.Add, recoveryBonus, RecoveryDecoratorId, Id));
        }

        public override void RemoveUpgrade(AresBlessing ability)
        {
            ability.RemoveParameterDecorator(HealthDecoratorId, AresBlessing.Parameters.HealthBonus);
            ability.RemoveParameterDecorator(RecoveryDecoratorId, AresBlessing.Parameters.RecoveryBonus);
        }

        public override IAbilityUpgrade Copy() => new ArUpgradeBlessingBonus(Id, Tags, Tier, healthBonus, recoveryBonus);
    }
}
