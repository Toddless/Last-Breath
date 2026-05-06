namespace Battle.Source.Abilities.PoisonCoating
{
    using Core.Interfaces.Abilities;

    /// <summary>
    /// L3 upgrade: when the coating is applied, also grants a regeneration stack to the caster.
    /// </summary>
    public class PcUpgradeRegenOnHit(string id, string[] tags, int tier,
        float regenAmount = 20f, int regenDuration = 3)
        : AbilityUpgrade<PoisonCoating>(id, tags, tier)
    {

        public override void ApplyUpgrade(PoisonCoating ability)
        {
        }

        public override void RemoveUpgrade(PoisonCoating ability)
        {
        }

        public override IAbilityUpgradeWrap<PoisonCoating> Clone() =>
            new PcUpgradeRegenOnHit(Id, Tags, Tier, regenAmount, regenDuration);
    }
}
