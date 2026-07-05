namespace Battle.Source.Abilities.Armageddon
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Decorators;
    using HitDelivery;

    /// <summary>L3 upgrade: hits every enemy on the field at the price of a longer cooldown.</summary>
    public class ArmUpgradeAllTargets(string id, string[] tags, int tier, float additionalCooldown)
        : AbilityUpgrade<Armageddon>(id, tags, tier)
    {
        private const string CooldownDecoratorId = "Ability_Parameter_Decorator_Arm_AoE_Cooldown";
        private IHitSequenceStrategy? _previousDelivery;

        public override void ApplyUpgrade(Armageddon ability)
        {
            _previousDelivery = ability.HitSequence;
            ability.HitSequence = new AllEnemiesHits();
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.Cooldown, Priority.Weak, OperationType.Add, additionalCooldown, CooldownDecoratorId, Id));
        }

        public override void RemoveUpgrade(Armageddon ability)
        {
            if (_previousDelivery != null)
            {
                ability.HitSequence = _previousDelivery;
                _previousDelivery = null;
            }

            ability.RemoveParameterDecorator(CooldownDecoratorId, AbilityParameter.Cooldown);
        }

        public override IAbilityUpgrade Copy() => new ArmUpgradeAllTargets(Id, Tags, Tier, additionalCooldown);
    }
}
