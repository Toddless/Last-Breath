namespace Battle.Source.Abilities
{
    using System;
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>Generic upgrade: the cast additionally lays an effect on its TARGETS, built fresh per
    /// activation. The debuff counterpart of <see cref="AbilityUpgradeCastEffect"/>.</summary>
    public class AbilityUpgradeCastDebuff(string id, string[] tags, int tier, Func<Ability, IEffect?> effectFactory)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability) =>
            ability.ActivationRiders.TryAdd(Id, new DeferredEffectActivationRider(Id, () => effectFactory(ability), applyOnCaster: false, applyOnTargets: true));

        public override void RemoveUpgrade(Ability ability) => ability.ActivationRiders.Remove(Id);

        public override IAbilityUpgrade Copy() => new AbilityUpgradeCastDebuff(Id, Tags, Tier, effectFactory);
    }
}
