namespace Core.Items.Grants
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Battle.Abilities;
    using Battle.Skills;
    using Entity;
    using Events;

    /// <summary>Grants a standing effect while the item is worn. The battle-end cleanup wipes ALL
    /// effects (RemoveAllEffects in the owner's OnBattleEnds), so the grant re-applies a fresh
    /// instance on every battle start; outside battle the effect only exists until the first
    /// cleanup — by design, effects are battle-scoped. Battle-long duration is the author's job
    /// (a high "duration" in the payload).</summary>
    public class EffectGrant(
        string id,
        string effectId,
        IReadOnlyDictionary<string, float> properties,
        Func<IEffectProvider?> providerAccessor,
        Func<IGameEventBus?> busAccessor) : IItemGrant
    {
        private IFightable? _owner;
        private IEffect? _current;
        private IGameEventBus? _bus;

        public string Id => id;

        public string Description
        {
            get
            {
                field = providerAccessor()?.CreateEffect(EffectId, new RecordProperties(effectId, properties))?.Description ?? string.Empty;
                return field;
            }
        }

        /// <summary>Read access for serialization (save system round-trips the grant).</summary>
        public string EffectId => effectId;

        /// <summary>Numeric effect parameters from item JSON; read access for serialization.</summary>
        public IReadOnlyDictionary<string, float> Properties => properties;

        public void Attach(IFightable owner)
        {
            _owner = owner;
            ApplyEffect();
            _bus = busAccessor();
            _bus?.Subscribe<BattleInitializedEvent>(OnBattleInitialized);
        }

        public void Detach(IFightable owner)
        {
            _bus?.Unsubscribe<BattleInitializedEvent>(OnBattleInitialized);
            _bus = null;
            _current?.Remove();
            _current = null;
            _owner = null;
        }

        public IItemGrant Copy() => new EffectGrant(id, effectId, properties, providerAccessor, busAccessor);

        // Properties are the numeric payload the effect factory consumes: scaling every float here
        // is exactly "the granted effect gets +15%" (ascension). Keys stay untouched.
        public IItemGrant WithScaledValues(float factor) =>
            new EffectGrant(id, effectId,
                properties.ToDictionary(property => property.Key, property => property.Value * factor),
                providerAccessor, busAccessor);

        private void OnBattleInitialized(BattleInitializedEvent evnt)
        {
            if (_owner == null) return;
            if (!ReferenceEquals(evnt.Player, _owner) && !evnt.Entities.Contains(_owner)) return;
            ApplyEffect();
        }

        private void ApplyEffect()
        {
            if (_owner == null || IsStillCarried()) return;
            var effect = providerAccessor()?.CreateEffect(effectId, new RecordProperties(effectId, properties));
            if (effect == null) return;
            _current = effect;
            // Self-application: the wearer is both caster and target, so the usual caster/target
            // pipelines (mutators, resistances) still see the instance. Apply is synchronous today.
            _ = effect.Apply(new EffectApplyingContext { Caster = _owner, Target = _owner, Source = id });
        }

        // The cleanup removes the instance from the component but our reference survives —
        // membership in the owner's live list is the truth, not the reference itself.
        private bool IsStillCarried() =>
            _current != null && _owner != null && _owner.Effects.Effects.Contains(_current);
    }
}
