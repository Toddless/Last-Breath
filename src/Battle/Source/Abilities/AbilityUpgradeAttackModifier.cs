namespace Battle.Source.Abilities
{
    using Core.Battle;
    using Core.Battle.Abilities;

    /// <summary>Generic upgrade: installs an attack context modifier on any ability that owns an attack
    /// pipeline. Seated on one that does not, it is inert — a tag promises a fit, not a result.</summary>
    public class AbilityUpgradeAttackModifier(string id, string[] tags, int tier, IAttackModifier modifier)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability)
        {
            if (ability is IAttackModifierHost host) host.AddAttackModifier(modifier);
        }

        public override void RemoveUpgrade(Ability ability)
        {
            if (ability is IAttackModifierHost host) host.RemoveAttackModifier(modifier.Id);
        }

        public override IAbilityUpgrade Copy() => new AbilityUpgradeAttackModifier(Id, Tags, Tier, modifier);
    }
}
