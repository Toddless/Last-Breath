namespace Battle.Source.Abilities.Armageddon
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Effects;

    /// <summary>L2 upgrade: stage 3 additionally puts burning stacks on every hit target.</summary>
    public class AugmentArmStage3Burning(string id, string[] tags, int tier, int stacks, int duration, float damageMultiplier)
        : Augment<Armageddon>(id, tags, tier)
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

        public override IAugment Copy() => new AugmentArmStage3Burning(Id, Tags, Tier, stacks, duration, damageMultiplier);
    }
}
