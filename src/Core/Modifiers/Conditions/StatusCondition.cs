namespace Core.Modifiers.Conditions
{
    using Entity;
    using Enums;
    using Events;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// "While any of these statuses is on me". One class covers a single status and every aggregate —
    /// the group is a mask, exactly like the damage-over-turn knobs read theirs — and the negated form
    /// covers "while clean".
    /// </summary>
    public sealed class StatusCondition(StatusEffects mask) : OwnerCondition
    {
        protected override bool Evaluate(bool wasMet) => Owner != null && (Owner.StatusEffects & mask) != 0;

        protected override void Subscribe(IFightable owner)
        {
            owner.CombatEvents.Subscribe<StatusEffectAppliedEvent>(OnStatusApplied);
            owner.CombatEvents.Subscribe<StatusEffectRemovedEvent>(OnStatusRemoved);
        }

        protected override void Unsubscribe(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<StatusEffectAppliedEvent>(OnStatusApplied);
            owner.CombatEvents.Unsubscribe<StatusEffectRemovedEvent>(OnStatusRemoved);
        }

        private void OnStatusApplied(StatusEffectAppliedEvent applied) => Reevaluate();

        private void OnStatusRemoved(StatusEffectRemovedEvent removed) => Reevaluate();
    }

    /// <summary>
    /// The gate every record over statuses passes: the names are resolved into one mask — plain statuses
    /// and aggregate aliases alike — and a record that names nothing at all is reported and refused instead
    /// of becoming a predicate no status can satisfy. Each type only says what it builds once the mask is
    /// known to hold something, which is the same division the vital records are read with.
    /// </summary>
    public abstract class StatusMaskConditionFactory : IConditionFactory
    {
        public abstract string Type { get; }

        public OwnerCondition? Create(JObject json)
        {
            var mask = StatusEffects.None;
            foreach (string? name in json[ConditionFields.Statuses]?.Values<string>() ?? [])
                mask |= StatusMasks.Resolve(name ?? string.Empty);

            if (mask != StatusEffects.None) return Create(mask);

            Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Statuses}' names no status at all");
            return null;
        }

        /// <summary>Builds the predicate for a mask that is known to name something.</summary>
        protected abstract OwnerCondition Create(StatusEffects mask);
    }

    public class StatusConditionFactory : StatusMaskConditionFactory
    {
        public override string Type => ConditionTypes.Status;

        protected override OwnerCondition Create(StatusEffects mask) => new StatusCondition(mask);
    }
}
