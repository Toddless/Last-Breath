namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Decorators;

    /// <summary>
    /// L2 upgrade: increases buff effectiveness while reducing the number of LightStep stacks applied
    /// </summary>
    public class DsUpgradeAddEffectivenessReduceStacks(string id, string[] tags, int tier, float additionalEffectiveness, int amountStacks)
        : AbilityUpgrade<DarkShroud>(id, tags, tier)
    {
        private const string EffectivenessDecoratorId = "Ability_Parameter_Decorator_Ds_Effectiveness";
        private const string StacksDecoratorId = "Ability_Parameter_Decorator_Ds_Stacks";

        public override void ApplyUpgrade(DarkShroud ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<DarkShroud.Parameters>(
                DarkShroud.Parameters.Effectiveness, Priority.Weak, OperationType.Add, additionalEffectiveness, EffectivenessDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<DarkShroud.Parameters>(
                DarkShroud.Parameters.Stacks, Priority.Weak, OperationType.Subtract, amountStacks, StacksDecoratorId, Id));
        }

        public override void RemoveUpgrade(DarkShroud ability)
        {
            ability.RemoveParameterDecorator(EffectivenessDecoratorId, DarkShroud.Parameters.Effectiveness);
            ability.RemoveParameterDecorator(StacksDecoratorId, DarkShroud.Parameters.Stacks);
        }

        public override IAbilityUpgrade Copy() => new DsUpgradeAddEffectivenessReduceStacks(Id, Tags, Tier, additionalEffectiveness, amountStacks);
    }
}
