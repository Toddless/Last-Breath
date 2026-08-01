namespace Core.Modifiers.Conditions
{
    using System;
    using System.Collections.Generic;
    using Battle;
    using Entity;
    using Events;

    /// <summary>
    /// Shared lifecycle of every predicate whose subject is the turn the owner is in — "the first strike
    /// of the turn", "nothing has hurt me this turn", "the fight has run this long".
    /// <para>Such a predicate has no subject outside a fight: a turn is something a battle hands a fighter,
    /// and out of combat — or in a battle that has not reached the owner yet — there is no turn to read and
    /// therefore no answer at all. The predicate is met by nothing then, inverted or not, exactly like an
    /// unattached one. That is the whole of the guard around the shape this family is written in: "I have
    /// not attacked this turn" is the inverted form of a tally, and a tally with no turn behind it would put
    /// every such line permanently on — on a character standing in a field, most of all.</para>
    /// <para>The turn is taken from the owner's own combat bus. <see cref="TurnStartEvent"/> is published on
    /// three buses at once: the fighter's own, the battle's and the game's. Only the first carries his turns
    /// alone — the other two carry the turn of whoever is playing — so a reset taken there would rearm the
    /// line on every enemy's turn as well. The event names whose turn it is and this family reads that name,
    /// so what the bus already guarantees is also what the predicate checks.</para>
    /// <para>The combat bus lives and dies with the fighter rather than with the fight, so nothing here
    /// outlives a battle bus; the end of the battle arrives on the same bus (the fighter republishes it) and
    /// puts the predicate back to having no turn to read.</para>
    /// </summary>
    public abstract class TurnCondition : OwnerCondition
    {
        /// <summary>How to release every subscription taken in <see cref="Subscribe"/>, each closed over the
        /// very delegate that was handed to the bus. Filled at subscribe and drained at unsubscribe, so a
        /// predicate following four signals cannot leave one of them listening.</summary>
        private List<Action<ICombatEventBus>> _release = [];

        /// <summary>Which turn of the current battle the owner is in, counted from one. Zero while there is
        /// no fight, or while there is one the owner has not been given a turn in yet.</summary>
        protected int Turn { get; private set; }

        /// <summary>No turn, nothing to read — see the class summary.</summary>
        protected override bool CanAnswer => Turn > 0;

        /// <summary>What the derived predicate counts within a turn, on top of the turn itself. Everything
        /// it follows goes through <see cref="Follow{TEvent}"/> and is released with the rest.</summary>
        protected virtual void FollowTurnSpending(ICombatEventBus bus)
        {
        }

        /// <summary>Drops whatever the derived predicate counted inside one turn. Called wherever the turn
        /// itself is dropped: a turn starting, the battle ending, the fighter being released, a clone being
        /// handed out.</summary>
        protected virtual void ResetTurn()
        {
        }

        /// <summary>Listens to one event of the owner's and remembers how to stop listening to it.</summary>
        protected void Follow<TEvent>(ICombatEventBus bus, Action<TEvent> handler)
            where TEvent : notnull, ICombatEvent
        {
            bus.Subscribe(handler);
            _release.Add(released => released.Unsubscribe(handler));
        }

        /// <summary>A list of its own, never the one a clone was copied from field by field: what this
        /// predicate takes on is released by this predicate and by nobody else.</summary>
        protected sealed override void Subscribe(IFightable owner)
        {
            _release = [];
            Follow<TurnStartEvent>(owner.CombatEvents, OnTurnStarted);
            Follow<BattleEndEvent>(owner.CombatEvents, OnBattleEnded);
            FollowTurnSpending(owner.CombatEvents);
        }

        protected sealed override void Unsubscribe(IFightable owner)
        {
            foreach (Action<ICombatEventBus> release in _release)
                release(owner.CombatEvents);

            _release.Clear();
        }

        protected override void Forget()
        {
            Turn = 0;
            ResetTurn();
        }

        /// <summary>A turn of the owner's own: whoever else's turn is announced leaves the tally alone.</summary>
        private void OnTurnStarted(TurnStartEvent started)
        {
            if (!ReferenceEquals(started.StartedTurn, Owner)) return;

            Turn++;
            ResetTurn();
            Reevaluate();
        }

        /// <summary>The fight is over and there is no turn to read again, so a line held up by one lets go
        /// instead of carrying the last turn's verdict into the world and into the next battle.</summary>
        private void OnBattleEnded(BattleEndEvent ended)
        {
            Turn = 0;
            ResetTurn();
            Reevaluate();
        }
    }
}
