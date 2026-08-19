namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Abilities;
    using Core.Localization;
    using Godot;

    public abstract class Augment<T>(string id, string[] tags, int tier) : IAugmentWrap<T>
        where T : IAbility
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = tags;
        public Texture2D? Icon { get; }
        public int Tier { get; } = tier;
        public bool Learned { get; private set; }
        public IReadOnlyDictionary<string, object?> DescriptionValues { get; set; } = new Dictionary<string, object?>();

        public string Description => DescriptionValues.Count > 0
            ? Localization.RenderDescription(Id, DescriptionValues, TextFormat.Rich)
            : Localization.LocalizeDescription(Id);

        public string DisplayName => Localization.Localize(Id);

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        /// <summary>The name THIS COPY's rider is seated under — see <see cref="RiderKeys"/>. Keyed by
        /// record the two copies were one: the first to arrive installed the only rider and the first to
        /// leave took it away from the other.</summary>
        protected string RiderKey(string riderId) => RiderKeys.Of(riderId, InstanceId);

        /// <summary>Non-generic dispatch (see IAbilityUpgrade): the compatibility check lives here, once.</summary>
        public void Apply(IAbility ability)
        {
            if (ability is T typed)
            {
                ApplyUpgrade(typed);
                Learned = true;
            }
            else Tracker.TrackException($"Upgrade '{Id}' expects {typeof(T).Name}", new InvalidCastException(), this);
        }

        public void Remove(IAbility ability)
        {
            if (ability is not T typed) return;

            RemoveUpgrade(typed);
            Learned = false;
        }

        public abstract void ApplyUpgrade(T ability);

        public abstract void RemoveUpgrade(T ability);

        public abstract IAugment Copy();
    }
}
