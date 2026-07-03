namespace Battle.Source.Abilities.PoisonCoating
{
    using System;
    using Core.Interfaces.Abilities;
    using Modifiers;

    /// <summary>
    /// L3 upgrade: while the coating buff is active, each successful attack additionally
    /// applies the debuff produced by <c>debuffFactory</c> to the target.
    /// </summary>
    public class PcUpgradeApplyDebuffOnHit(string id, string[] tags, int tier, Func<IEffect> debuffFactory)
        : AbilityUpgrade<PoisonCoating>(id, tags, tier)
    {
        private readonly IAbilityPostActivationModifier _modifier = new AbilityDebuffPostActivationModifier(debuffFactory());

        public override void ApplyUpgrade(PoisonCoating ability) => ability.PostActivationEffect.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(PoisonCoating ability) => ability.PostActivationEffect.Remove(_modifier.Id);

        public override IAbilityUpgradeWrap<PoisonCoating> Copy() =>
            new PcUpgradeApplyDebuffOnHit(Id, Tags, Tier, debuffFactory);
    }
}
