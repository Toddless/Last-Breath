namespace Battle.Source.Abilities.PoisonCoating
{
    using System;
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>
    /// L3 upgrade: while the coating buff is active, each successful attack additionally
    /// applies the debuff produced by <c>debuffFactory</c> to the target.
    /// </summary>
    public class PcUpgradeApplyDebuffOnHit(string id, string[] tags, int tier, Func<IEffect> debuffFactory)
        : AbilityUpgrade<PoisonCoating>(id, tags, tier)
    {
        private readonly IActivationRider _modifier = new AbilityDebuffActivationRider(debuffFactory());

        public override void ApplyUpgrade(PoisonCoating ability) => ability.ActivationRiders.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(PoisonCoating ability) => ability.ActivationRiders.Remove(_modifier.Id);

        public override IAbilityUpgradeWrap<PoisonCoating> Copy() =>
            new PcUpgradeApplyDebuffOnHit(Id, Tags, Tier, debuffFactory);
    }
}
