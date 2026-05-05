namespace Battle.Source.Abilities
{
    using Godot;
    using System;
    using Utilities;
    using Core.Interfaces.Abilities;

    public abstract class AbilityUpgrade<T>(string id, string[] tags, int tier) : IAbilityUpgradeWrap<T>
        where T : IAbility
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = tags;
        public Texture2D? Icon { get; }
        public int Tier { get; } = tier;
        public string Description => Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localize(Id);

        public event Action? AbilityUpgradeChanged;

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public abstract void ApplyUpgrade(T ability);

        public abstract void RemoveUpgrade(T ability);

        public abstract IAbilityUpgradeWrap<T> Clone();
    }
}
