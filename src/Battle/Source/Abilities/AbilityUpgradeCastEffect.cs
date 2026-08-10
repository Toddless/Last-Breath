namespace Battle.Source.Abilities
{
    using System;
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>
    /// Generic upgrade: the cast additionally applies a self-effect built fresh per activation
    /// (deferred factory — the effect sees the ability's current parameters at cast time).
    /// </summary>
    public class AbilityUpgradeCastEffect(string id, string[] tags, int tier, Func<Ability, IEffect?> effectFactory)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability) =>
            ability.ActivationRiders.TryAdd(Id, new DeferredEffectActivationRider(Id, () => effectFactory(ability)));

        public override void RemoveUpgrade(Ability ability) => ability.ActivationRiders.Remove(Id);

        public override IAbilityUpgrade Copy() => new AbilityUpgradeCastEffect(Id, Tags, Tier, effectFactory);
    }
}
