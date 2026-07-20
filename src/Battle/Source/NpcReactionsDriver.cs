namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Narrative.Facts;

    /// <summary>
    /// Battle-scoped driver of NPC combat reactions (the "reactions" section of Npc.json): for
    /// every attached fighter with reactions it instantiates the hidden abilities and listens to
    /// the owner's combat events. A trigger match rolls the chance behind three gates — per-turn
    /// cap, the "blocked by final death" world fact and a living owner in a running battle.
    /// Per-turn counters reset on every TurnStartEvent of the battle bus; Dispose detaches all.
    /// Pure C# — testable without Godot.
    /// </summary>
    public sealed class NpcReactionsDriver : IDisposable
    {
        private readonly IBattleField _field;
        private readonly IAbilityProvider _abilities;
        private readonly IWorldFactsService? _facts;
        private readonly IRandomNumberGenerator _rnd;
        private readonly Func<bool> _isBattleActive;
        private readonly IBattleEventBus? _battleEventBus;
        private readonly List<Entry> _entries = [];

        public NpcReactionsDriver(
            IBattleField field,
            IAbilityProvider abilities,
            IWorldFactsService? facts,
            IRandomNumberGenerator rnd,
            Func<bool> isBattleActive,
            IBattleEventBus? battleEventBus)
        {
            _field = field;
            _abilities = abilities;
            _facts = facts;
            _rnd = rnd;
            _isBattleActive = isBattleActive;
            _battleEventBus = battleEventBus;
            _battleEventBus?.Subscribe<TurnStartEvent>(OnTurnStart);
        }

        /// <summary>Wires the fighter's reactions if it has any; everyone else is a cheap no-op.
        /// Safe for latecomers — the arena calls it from TryJoinBattle as well.</summary>
        public void TryAttach(IFightable fighter)
        {
            if (fighter is not IFightableNpc npc || npc.Reactions.Count == 0) return;

            foreach (var reaction in npc.Reactions)
            {
                IAbility ability;
                try
                {
                    ability = _abilities.CreateAbility(reaction.AbilityId);
                }
                catch (KeyNotFoundException e)
                {
                    Tracker.TrackException($"Reaction ability '{reaction.AbilityId}' of '{npc.Id}' cannot be created", e, this);
                    continue;
                }

                ability.SetOwner(npc);
                var entry = new Entry(npc, reaction, ability);
                Action<DamageTakenEvent> handler = evt => OnDamageTaken(entry, evt);
                entry.Handler = handler;
                npc.CombatEvents.Subscribe(handler);
                _entries.Add(entry);
            }
        }

        /// <summary>The cap is per turn, not per battle: every new turn starts with fresh counters.</summary>
        public void ResetTurnCounters()
        {
            foreach (var entry in _entries)
                entry.FiredThisTurn = 0;
        }

        public void Dispose()
        {
            _battleEventBus?.Unsubscribe<TurnStartEvent>(OnTurnStart);
            foreach (var entry in _entries)
            {
                if (entry.Handler != null) entry.Owner.CombatEvents.Unsubscribe(entry.Handler);
                entry.Ability.RemoveOwner();
            }

            _entries.Clear();
        }

        private void OnTurnStart(TurnStartEvent evnt) => ResetTurnCounters();

        // async void is the event-handler boundary; the cast resolves instantly (replay model)
        // and any failure is contained here instead of tearing the publisher's loop.
        private async void OnDamageTaken(Entry entry, DamageTakenEvent evt)
        {
            try
            {
                if (!ShouldFire(entry, evt)) return;
                entry.FiredThisTurn++;
                await entry.Ability.Execute([], _field);
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Reaction '{entry.Config.AbilityId}' of '{entry.Owner.Id}' failed", e, this);
            }
        }

        private bool ShouldFire(Entry entry, DamageTakenEvent evt)
        {
            if (!_isBattleActive()) return false;
            if (!entry.Owner.IsAlive) return false; // the killing blow must not be avenged from the grave
            if (!MatchesTrigger(entry.Config.Trigger, evt)) return false;
            if (entry.FiredThisTurn >= entry.Config.MaxPerTurn) return false;
            if (IsBlockedByFinalDeath(entry.Config)) return false;
            return _rnd.RandFloat() <= entry.Config.Chance;
        }

        private bool IsBlockedByFinalDeath(NpcReactionConfig config) =>
            config.BlockedByFinalDeathOf is { } npcId
            && _facts?.IsSet(FactKeys.NpcFinalDeath(npcId)) == true;

        private static bool MatchesTrigger(ReactionTrigger trigger, DamageTakenEvent evt) => trigger switch
        {
            // Attacks only by design: the assist's own ability damage cannot chain reactions.
            ReactionTrigger.DamagedByAttack => evt.Context.Cause == DamageCause.Attack,
            _ => false
        };

        private sealed class Entry(IFightableNpc owner, NpcReactionConfig config, IAbility ability)
        {
            public IFightableNpc Owner { get; } = owner;
            public NpcReactionConfig Config { get; } = config;
            public IAbility Ability { get; } = ability;
            public int FiredThisTurn { get; set; }
            public Action<DamageTakenEvent>? Handler { get; set; }
        }
    }
}
