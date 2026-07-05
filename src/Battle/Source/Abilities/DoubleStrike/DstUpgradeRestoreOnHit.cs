namespace Battle.Source.Abilities.DoubleStrike
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Decorators;

    /// <summary>L3 upgrade: a landed first strike restores health, a landed second strike restores mana.</summary>
    public class DstUpgradeRestoreOnHit(string id, string[] tags, int tier, float healthRestore, float manaRestore)
        : AbilityUpgrade<DoubleStrike>(id, tags, tier)
    {
        private const string HealthDecoratorId = "Ability_Parameter_Decorator_Dst_Health_Restore";
        private const string ManaDecoratorId = "Ability_Parameter_Decorator_Dst_Mana_Restore";

        public override void ApplyUpgrade(DoubleStrike ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<DoubleStrike.Parameters>(
                DoubleStrike.Parameters.HealthRestore, Priority.Weak, OperationType.Add, healthRestore, HealthDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<DoubleStrike.Parameters>(
                DoubleStrike.Parameters.ManaRestore, Priority.Weak, OperationType.Add, manaRestore, ManaDecoratorId, Id));
        }

        public override void RemoveUpgrade(DoubleStrike ability)
        {
            ability.RemoveParameterDecorator(HealthDecoratorId, DoubleStrike.Parameters.HealthRestore);
            ability.RemoveParameterDecorator(ManaDecoratorId, DoubleStrike.Parameters.ManaRestore);
        }

        public override IAbilityUpgrade Copy() => new DstUpgradeRestoreOnHit(Id, Tags, Tier, healthRestore, manaRestore);
    }
}
