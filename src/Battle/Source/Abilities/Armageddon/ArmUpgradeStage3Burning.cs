namespace Battle.Source.Abilities.Armageddon
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Effects;

    /// <summary>L2 upgrade: stage 3 additionally puts burning stacks on every hit target.</summary>
    public class ArmUpgradeStage3Burning(string id, string[] tags, int tier, int stacks, int duration, float damageMultiplier)
        : AbilityUpgrade<Armageddon>(id, tags, tier)
    {
        public override void ApplyUpgrade(Armageddon ability)
        {
            ability.Stage3EffectFactory = () => new DamageOverTurnEffect(duration, StatusEffects.Burning, stacks, damageMultiplier);
            ability.Stage3EffectStacks = stacks;
        }

        public override void RemoveUpgrade(Armageddon ability)
        {
            ability.Stage3EffectFactory = null;
            ability.Stage3EffectStacks = 1;
        }

        public override IAbilityUpgrade Copy() => new ArmUpgradeStage3Burning(Id, Tags, Tier, stacks, duration, damageMultiplier);
    }
}
