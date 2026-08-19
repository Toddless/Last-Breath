namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Battle.Abilities;
    using Effects;
    using Riders;

    /// <summary>
    /// L3: for the ability's buff duration, the caster's critical chance becomes "Lucky".
    /// </summary>
    public class CcAugmentLuckyCrit(string id, string[] tags, int tier, int duration)
        : AbilityAugment<CriticalCalculation>(id, tags, tier)
    {
        private readonly IActivationRider _modifier =
            new AbilityBuffActivationRider(new LuckyCritChanceEffect(duration, 1));

        public override void ApplyUpgrade(CriticalCalculation ability) => ability.AddActivationRider(RiderKey(_modifier.Id), _modifier);

        public override void RemoveUpgrade(CriticalCalculation ability) => ability.RemoveActivationRider(RiderKey(_modifier.Id));

        public override IAbilityAugment Copy() => new CcAugmentLuckyCrit(Id, Tags, Tier, duration);
    }
}
