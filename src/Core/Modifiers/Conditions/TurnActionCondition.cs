namespace Core.Modifiers.Conditions
{
    using System;
    using System.Collections.Generic;
    using Battle;
    using Data;
    using Enums;
    using Events;
    using Newtonsoft.Json.Linq;

    /// <summary>What a turn-scoped predicate counts inside one turn of the owner's.</summary>
    public enum TurnAction : byte
    {
        /// <summary>Attacks of the owner's that were resolved against somebody.</summary>
        Attack,

        /// <summary>Abilities of the owner's that finished executing, riders included.</summary>
        Ability,

        /// <summary>Damage that landed on the owner, whatever caused it.</summary>
        DamageTaken,

        /// <summary>Attacks aimed at the owner and resolved against him — landed, evaded and blocked
        /// alike. Distinct from <see cref="DamageTaken"/> at both ends: a swing that touched nobody is
        /// still an attack on the owner, and poison ticking on him is damage nobody swung.</summary>
        AttackReceived,
    }

    /// <summary>
    /// "While the turn I am in already holds N of these". One counting predicate serves the whole
    /// turn-scoped family, and the inversion flag serves the half of it written as "the first ..." — the
    /// first strike of a turn is the strike made while the tally is still empty, which is the same
    /// sentence as "I have not attacked this turn".
    /// <para>What is counted is a SPENT action, never a joined one: an attack is counted once it is over
    /// and a cast once it has finished. What shapes the numbers of an action is read while that action is
    /// in flight, so counting on the way in would hand the bonus to the second action of the turn instead
    /// of the first. The same reason puts the tally of incoming damage after the hit has landed rather
    /// than before it.</para>
    /// </summary>
    public sealed class TurnActionCondition(TurnAction action, int count) : TurnCondition
    {
        /// <summary>One action as the family needs it: which of the owner's events spend it. Everything an
        /// action is made of is one entry, so an action cannot arrive half-wired — counted but following
        /// nothing, which would leave the inverted form ("I have not ...") on for the whole fight.</summary>
        private static readonly Dictionary<TurnAction, Action<TurnActionCondition, ICombatEventBus>> s_spending = new()
        {
            [TurnAction.Attack] = (condition, bus) => condition.Follow<AfterAttackEvent>(bus, condition.OnSpent),
            [TurnAction.Ability] = (condition, bus) => condition.Follow<AbilityExecutedEvent>(bus, condition.OnSpent),
            [TurnAction.DamageTaken] = (condition, bus) => condition.Follow<DamageTakenEvent>(bus, condition.OnSpent),
            [TurnAction.AttackReceived] = (condition, bus) =>
            {
                condition.Follow<DamageTakenEvent>(bus, condition.OnAttackLanded);
                condition.Follow<AttackEvadedEvent>(bus, condition.OnSpent);
                condition.Follow<AttackBlockedEvent>(bus, condition.OnSpent);
            },
        };

        private readonly Action<TurnActionCondition, ICombatEventBus> _spending = Spent(action);

        private int _spent;

        /// <summary>The actions this family can count. A record names exactly one of these.</summary>
        public static IReadOnlyCollection<TurnAction> Countable => s_spending.Keys;

        /// <summary>Whether the family can count this action at all — the gate the factory applies to
        /// data before the constructor applies it to code.</summary>
        public static bool CanCount(TurnAction action) => s_spending.ContainsKey(action);

        protected override bool Evaluate(bool wasMet) => _spent >= count;

        protected override void FollowTurnSpending(ICombatEventBus bus) => _spending(this, bus);

        protected override void ResetTurn() => _spent = 0;

        /// <summary>The same gate the factory applies, held by the family itself: a predicate built from C#
        /// on an action nothing spends would subscribe to nothing and answer "none of these yet" for the
        /// whole fight. Data never reaches the throw — the factory reports and refuses first — so what is
        /// left here is a wiring mistake, and it surfaces where it was made.</summary>
        private static Action<TurnActionCondition, ICombatEventBus> Spent(TurnAction action) =>
            s_spending.TryGetValue(action, out Action<TurnActionCondition, ICombatEventBus>? spending)
                ? spending
                : throw new ArgumentOutOfRangeException(nameof(action), action, $"A turn predicate counts one of {string.Join(", ", s_spending.Keys)}");

        private void OnSpent<TEvent>(TEvent spent)
            where TEvent : notnull, ICombatEvent => Add();

        /// <summary>An attack that got through is damage the attack caused; damage of any other cause was
        /// swung by nobody and is no attack on the owner.</summary>
        private void OnAttackLanded(DamageTakenEvent landed)
        {
            if (landed.Context.Cause == DamageCause.Attack) Add();
        }

        private void Add()
        {
            _spent++;
            Reevaluate();
        }
    }

    /// <summary>
    /// The gate every record over a turn action passes: the action is parsed strictly and then checked
    /// against what the family can actually count, and the tally it is compared against must ask for at
    /// least one — a record met by everyone is the inversion flag, not a count of zero.
    /// </summary>
    public class TurnActionConditionFactory : IConditionFactory
    {
        private const int DefaultCount = 1;

        public string Type => ConditionTypes.TurnAction;

        public OwnerCondition? Create(JObject json)
        {
            var action = EnumParser.ParseEnum<TurnAction>(json.Value<string>(ConditionFields.Action) ?? string.Empty);
            if (!TurnActionCondition.CanCount(action))
            {
                Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Action}' is '{action}', and a turn predicate counts one of {string.Join(", ", TurnActionCondition.Countable)}");
                return null;
            }

            int count = json.Value<int?>(ConditionFields.Count) ?? DefaultCount;
            if (count >= DefaultCount) return new TurnActionCondition(action, count);

            Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Count}' is {count} — a count below one is met by everyone, invert the entry instead");
            return null;
        }
    }
}
