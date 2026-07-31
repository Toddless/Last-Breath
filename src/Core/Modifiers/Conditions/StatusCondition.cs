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

    public class StatusConditionFactory : IConditionFactory
    {
        public string Type => ConditionTypes.Status;

        public OwnerCondition? Create(JObject json)
        {
            var mask = StatusEffects.None;
            foreach (string? name in json[ConditionFields.Statuses]?.Values<string>() ?? [])
                mask |= StatusMasks.Resolve(name ?? string.Empty);

            if (mask != StatusEffects.None) return new StatusCondition(mask);

            Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Statuses}' names no status at all");
            return null;
        }
    }
}
