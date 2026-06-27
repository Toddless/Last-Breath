namespace Battle.Source.Abilities
{
    using System;
    using Core.Interfaces.Abilities;

    public class DelegateUpgrade<T>(string id, string[] tags, int tier, Action<T> add, Action<T> remove)
        : AbilityUpgrade<T>(id, tags, tier) where T : IAbility
    {
        public override void ApplyUpgrade(T ability) => add(ability);
        public override void RemoveUpgrade(T ability) => remove(ability);
        public override IAbilityUpgradeWrap<T> Copy() => new DelegateUpgrade<T>(Id, Tags, Tier, add, remove);
    }
}
