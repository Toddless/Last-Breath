namespace Battle.Source.Abilities
{
    using System;
    using Core.Battle.Abilities;

    public class DelegateAugment<T>(string id, string[] tags, int tier, Action<T> add, Action<T> remove)
        : Augment<T>(id, tags, tier) where T : IAbility
    {
        public override void ApplyUpgrade(T ability) => add(ability);
        public override void RemoveUpgrade(T ability) => remove(ability);
        public override IAugmentWrap<T> Copy() => new DelegateAugment<T>(Id, Tags, Tier, add, remove);
    }
}
