namespace Core.Modifiers.Conditions
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Entity;
    using Enums;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Shared plumbing of every predicate over one of the owner's vitals: which signal to listen to and
    /// how to read the pair (current, maximum). Both ends move — spending changes the current value and
    /// re-gearing changes the maximum — so the condition follows the resource signal AND the parameter
    /// behind the maximum.
    /// </summary>
    public abstract class ResourceCondition(Costs resource) : OwnerCondition
    {
        /// <summary>One vital as the family needs it: the signal that announces a change, the release of
        /// that same signal, the pair of values to read and the parameter carrying the maximum. Everything
        /// about a resource is one entry, so a vital cannot arrive half-wired — followed but never read,
        /// or read while no signal ever reaches the predicate.</summary>
        private sealed record Vital(
            EntityParameter Maximum,
            Action<IFightable, Action<float>> Follow,
            Action<IFightable, Action<float>> Release,
            Func<IFightable, (float Current, float Max)> Read);

        private static readonly Dictionary<Costs, Vital> s_vitals = new()
        {
            [Costs.Health] = new Vital(
                EntityParameter.Health,
                (owner, changed) => owner.CurrentHealthChanged += changed,
                (owner, changed) => owner.CurrentHealthChanged -= changed,
                owner => (owner.CurrentHealth, owner.Parameters.MaxHealth)),
            [Costs.Mana] = new Vital(
                EntityParameter.Mana,
                (owner, changed) => owner.CurrentManaChanged += changed,
                (owner, changed) => owner.CurrentManaChanged -= changed,
                owner => (owner.CurrentMana, owner.Parameters.MaxMana)),
            [Costs.Barrier] = new Vital(
                EntityParameter.Barrier,
                (owner, changed) => owner.CurrentBarrierChanged += changed,
                (owner, changed) => owner.CurrentBarrierChanged -= changed,
                owner => (owner.CurrentBarrier, owner.Parameters.MaxBarrier)),
        };

        private readonly Vital _vital = Followed(resource);

        /// <summary>Share of the maximum the owner currently holds — and nothing at all when the owner has
        /// no such vital. A fighter without a barrier holds no share of one, which is a different answer
        /// from holding none of it: "there is no barrier" is a boundary and belongs to
        /// <see cref="ResourceStateCondition"/>, which answers it as empty on purpose.</summary>
        protected float? Fraction => Read() switch
        {
            (_, <= 0f) => null,
            var (current, max) => current / max,
        };

        /// <summary>The vitals this family can follow. A predicate takes one signal and reads one pair of
        /// values, so a record names exactly one of these and nothing else.</summary>
        public static IReadOnlyCollection<Costs> Followable => s_vitals.Keys;

        /// <summary>The same gate the factory applies, held by the family itself: a predicate built from
        /// C# on a resource nothing follows would subscribe to nothing and answer from an empty pair
        /// forever. Data never reaches the throw — the factory reports and refuses first — so what is left
        /// here is a wiring mistake, and it surfaces where it was made.</summary>
        private static Vital Followed(Costs resource) =>
            s_vitals.TryGetValue(resource, out var vital)
                ? vital
                : throw new ArgumentOutOfRangeException(nameof(resource), resource, $"A condition follows exactly one of {string.Join(", ", s_vitals.Keys)}");

        private void OnResourceChanged(float value) => Reevaluate();

        private void OnParameterChanged(EntityParameter parameter, float value)
        {
            if (_vital.Maximum == parameter) Reevaluate();
        }

        protected override void Subscribe(IFightable owner)
        {
            _vital.Follow(owner, OnResourceChanged);
            owner.Parameters.ParameterChanged += OnParameterChanged;
        }

        protected override void Unsubscribe(IFightable owner)
        {
            _vital.Release(owner, OnResourceChanged);
            owner.Parameters.ParameterChanged -= OnParameterChanged;
        }

        /// <summary>The resource as the owner holds it right now, paired with the maximum it is
        /// measured against.</summary>
        protected (float Current, float Max) Read() => Owner is { } owner ? _vital.Read(owner) : (0f, 0f);

        /// <summary>Whether the family can follow this resource at all. <see cref="Costs"/> is a flag enum,
        /// so a record can name a combination — or a plain number — that parses cleanly and points at no
        /// single vital.</summary>
        public static bool CanFollow(Costs resource) => s_vitals.ContainsKey(resource);
    }

    /// <summary>
    /// The gate every record over a vital passes: the resource name is parsed strictly and then checked
    /// against what the family can actually follow. Without the second half a predicate could be built on
    /// a resource it has no signal for — it would subscribe to nothing, read an empty pair and answer
    /// "below the threshold" forever, which is the silent always-on a broken record must never become.
    /// The family holds the same gate in its constructor; data stops here, with a report instead of an
    /// exception. Each type only says what it builds once the resource is known to be readable.
    /// </summary>
    public abstract class ResourceConditionFactory : IConditionFactory
    {
        public abstract string Type { get; }

        public OwnerCondition? Create(JObject json) => TryReadResource(json, out var resource) ? Create(json, resource) : null;

        /// <summary>Builds the predicate for a resource the family is known to follow.</summary>
        protected abstract OwnerCondition? Create(JObject json, Costs resource);

        private bool TryReadResource(JObject json, out Costs resource)
        {
            resource = EnumParser.ParseEnum<Costs>(json.Value<string>(ConditionFields.Resource) ?? string.Empty);
            if (ResourceCondition.CanFollow(resource)) return true;

            Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Resource}' is '{resource}', and a condition follows exactly one of {string.Join(", ", ResourceCondition.Followable)}");
            return false;
        }
    }
}
