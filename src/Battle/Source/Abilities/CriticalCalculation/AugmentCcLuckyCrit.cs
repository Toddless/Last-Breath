namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Battle.Abilities;
    using Effects;
    using Riders;

    /// <summary>
    /// For the ability's buff duration, the caster's critical chance becomes "Lucky".
    /// </summary>
    public class AugmentCcLuckyCrit(string id, string[] tags, int tier, int duration)
        : Augment<CriticalCalculation>(id, tags, tier)
    {
        private readonly IActivationRider _modifier =
            new AbilityBuffActivationRider(new LuckyCritChanceEffect(duration, 1));

        public override void ApplyUpgrade(CriticalCalculation ability) => ability.AddActivationRider(RiderKey(_modifier.Id), _modifier);

        public override void RemoveUpgrade(CriticalCalculation ability) => ability.RemoveActivationRider(RiderKey(_modifier.Id));

        public override IAugment Copy() => new AugmentCcLuckyCrit(Id, Tags, Tier, duration);
    }
}
