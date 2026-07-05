namespace Battle.Source.Abilities.AresBlessing
{
    using System;
    using Core.Interfaces.Abilities;
    using Riders;

    /// <summary>
    /// L3 upgrades: the cast additionally applies an extra self-effect (incoming damage reduction,
    /// turn-end heal or a damage buff). The factory receives the ability so the effect is built with
    /// the CURRENT duration at cast time.
    /// </summary>
    public class ArUpgradeAdditionalCastEffect(string id, string[] tags, int tier, Func<AresBlessing, IEffect> effectFactory)
        : AbilityUpgrade<AresBlessing>(id, tags, tier)
    {
        public override void ApplyUpgrade(AresBlessing ability) =>
            ability.ActivationRiders.TryAdd(Id, new DeferredEffectActivationRider(Id, () => effectFactory(ability)));

        public override void RemoveUpgrade(AresBlessing ability) => ability.ActivationRiders.Remove(Id);

        public override IAbilityUpgrade Copy() => new ArUpgradeAdditionalCastEffect(Id, Tags, Tier, effectFactory);
    }
}
