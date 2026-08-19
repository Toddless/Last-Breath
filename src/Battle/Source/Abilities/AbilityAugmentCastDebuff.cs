namespace Battle.Source.Abilities
{
    using System;
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>Generic upgrade: the cast additionally lays an effect on its TARGETS, built fresh per
    /// activation. The debuff counterpart of <see cref="AbilityAugmentCastEffect"/>.</summary>
    public class AbilityAugmentCastDebuff(string id, string[] tags, int tier, Func<Ability, IEffect?> effectFactory)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability) =>
            ability.AddActivationRider(RiderKey(Id), new DeferredEffectActivationRider(Id, () => effectFactory(ability), applyOnCaster: false, applyOnTargets: true));

        public override void RemoveUpgrade(Ability ability) => ability.RemoveActivationRider(RiderKey(Id));

        public override IAbilityAugment Copy() => new AbilityAugmentCastDebuff(Id, Tags, Tier, effectFactory);
    }
}
