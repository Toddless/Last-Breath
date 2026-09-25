namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity;
    using Core.Events;

    /// <summary>The arena-side executor of summon requests: spawn a joined fighter, tear one down.</summary>
    public interface ISummonHandler
    {
        /// <summary>Spawns the summon and joins it to the battle beside the summoner (dynamic slot,
        /// the summoner's group, stats = the summoner's current values × share). Null = refused.</summary>
        IFightableNpc? SpawnSummon(IFightable summoner, string npcId, float statShare);

        /// <summary>Removes the summon and its slot from the field for good: no corpse, no world return.</summary>
        void RemoveSummon(IFightableNpc summon);
    }

    /// <summary>
    /// Battle-scoped driver of summons (the reactions-driver pattern): listens on every attached
    /// fighter's combat bus for <see cref="SummonRequestedEvent"/> and executes it through the
    /// handler, refilling the summoner's pack up to the per-summoner cap. Death removal is
    /// two-phased: the arena reports the death at resolve time, the node and slot are torn down by
    /// <see cref="CleanupDead"/> AFTER the presentation gate — the killing blow plays on a live
    /// anchor. Battle end dissolves everything regardless of the outcome. Pure C# — testable
    /// without Godot.
    /// </summary>
    public sealed class SummonService(ISummonHandler handler, Func<bool> isBattleActive) : IDisposable
    {
        private sealed class Entry(string summonerId, IFightableNpc npc)
        {
            public string SummonerId { get; } = summonerId;
            public IFightableNpc Npc { get; } = npc;
            public bool PendingCleanup { get; set; }
        }

        private readonly List<(IFightable Fighter, Action<SummonRequestedEvent> Handler)> _attached = [];
        private readonly List<Entry> _summons = [];

        /// <summary>Any fighter may carry a summoning ability — the subscription is cheap.
        /// Safe for latecomers and freshly spawned summons alike.</summary>
        public void TryAttach(IFightable fighter)
        {
            if (_attached.Any(entry => entry.Fighter.IsSame(fighter.InstanceId))) return;

            Action<SummonRequestedEvent> onRequested = OnSummonRequested;
            fighter.CombatEvents.Subscribe(onRequested);
            _attached.Add((fighter, onRequested));
        }

        /// <summary>Resolve-time death notice (idempotent — the death event reaches the battle bus
        /// twice: at resolve and at the director's replay republish).</summary>
        public void OnSummonDied(IFightable entity)
        {
            var entry = _summons.FirstOrDefault(summon => summon.Npc.IsSame(entity.InstanceId));
            if (entry != null) entry.PendingCleanup = true;
        }

        /// <summary>Called after the presentation gate: the death has been shown, the slot and the
        /// node can go (a summon leaves no corpse — the slot dies with it).</summary>
        public void CleanupDead()
        {
            foreach (var entry in _summons.Where(summon => summon.PendingCleanup).ToList())
            {
                handler.RemoveSummon(entry.Npc);
                _summons.Remove(entry);
            }
        }

        /// <summary>The battle is over (ANY outcome, the player's flight included): summons dissolve
        /// like spells and never return to the world.</summary>
        public void DespawnAll()
        {
            foreach (var entry in _summons)
                handler.RemoveSummon(entry.Npc);
            _summons.Clear();
        }

        public void Dispose()
        {
            foreach (var (fighter, onRequested) in _attached)
                fighter.CombatEvents.Unsubscribe(onRequested);
            _attached.Clear();
            _summons.Clear();
        }

        private void OnSummonRequested(SummonRequestedEvent evt)
        {
            if (!isBattleActive() || !evt.Summoner.IsAlive) return;

            int alive = _summons.Count(summon =>
                summon.SummonerId == evt.Summoner.InstanceId && summon.Npc.IsAlive && !summon.PendingCleanup);
            int missing = Math.Min(evt.Count, evt.MaxAlivePerSummoner - alive);

            for (int i = 0; i < missing; i++)
            {
                var summon = handler.SpawnSummon(evt.Summoner, evt.NpcId, evt.StatShare);
                if (summon == null) return; // the field refused (no wiring / battle over) — stop asking
                _summons.Add(new Entry(evt.Summoner.InstanceId, summon));
            }
        }
    }
}
