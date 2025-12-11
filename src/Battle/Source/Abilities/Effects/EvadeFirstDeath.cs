namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class EvadeFirstDeath(
        string id,
        int duration,
        float percentToRecover,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks: 1, statusEffect)
    {
        private IEntity? _owner;
        public float PercentToRecover { get; } = percentToRecover;

        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            _owner = context.Target;
            _owner.CurrentHealthChanged += OnCurrentHealthChanges;
        }

        private void OnCurrentHealthChanges(float value)
        {
            if (value > 0) return;
            if (_owner == null) return;
            float toRecover = _owner.Parameters.MaxHealth * PercentToRecover;
            _owner.Heal(toRecover);
            Remove(_owner);
        }

        public override void Remove(IEntity source)
        {
            base.Remove(source);
            source.CurrentHealthChanged -= OnCurrentHealthChanges;
            _owner = null;
        }

        public override IEffect Clone() => new EvadeFirstDeath(Id, Duration, PercentToRecover);
    }
}
