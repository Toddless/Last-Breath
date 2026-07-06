namespace Battle.Source.Npc
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Ai.World.Skirmish;
    using Core.Components;
    using Core.Enums;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;

    /// <summary>Runs the abstract NPC-vs-NPC skirmishes; ticked by NpcWorldDirector.</summary>
    public interface INpcSkirmishService
    {
        /// <summary>Starts a skirmish between the two NPCs' squads if both sides are free and hostile.</summary>
        bool TryStart(ISkirmishParticipant initiator, ISkirmishParticipant target);

        void Tick(float delta);
    }

    /// <summary>
    /// Owns the active skirmishes: expands both sides to their squads, freezes the participants
    /// (IsFighting suspends brains and blocks player battles), republishes round/end events to
    /// the game bus and applies the outcome — losers die into their body lifecycles, undead
    /// losers get burned by living winners with a chance.
    /// </summary>
    public class NpcSkirmishService(IGameEventBus gameEventBus, IFactionRelationService relations) : INpcSkirmishService
    {
        private readonly List<NpcSkirmish> _active = [];
        private readonly IRandomNumberGenerator _rnd = new DefaultRandomNumberGenerator();
        private readonly SkirmishConfig _config = new();

        public bool TryStart(ISkirmishParticipant initiator, ISkirmishParticipant target)
        {
            if (!CanFight(initiator) || !CanFight(target)) return false;
            if (initiator.Group != null && ReferenceEquals(initiator.Group, target.Group)) return false;
            if (!relations.IsHostile(initiator.Fraction, target.Fraction) &&
                !relations.IsHostile(target.Fraction, initiator.Fraction)) return false;

            var sideA = ExpandSquad(initiator);
            var sideB = ExpandSquad(target);
            if (sideA.Count == 0 || sideB.Count == 0) return false;

            foreach (var participant in sideA.Concat(sideB))
                participant.IsFighting = true;

            var skirmish = new NpcSkirmish(sideA, sideB, _rnd, _config);
            skirmish.RoundResolved += round => OnRoundResolved(skirmish, round);
            skirmish.Completed += OnCompleted;
            _active.Add(skirmish);
            return true;
        }

        public void Tick(float delta)
        {
            // Reverse loop: completed skirmishes remove themselves inside their Completed handler.
            for (int i = _active.Count - 1; i >= 0; i--)
                _active[i].Tick(delta);
        }

        private static bool CanFight(ISkirmishParticipant npc) => npc.IsAlive && !npc.IsFighting;

        /// <summary>The whole group joins the fight (same rule as player encounters with grouped NPCs).</summary>
        private static List<ISkirmishParticipant> ExpandSquad(ISkirmishParticipant member)
        {
            if (member.Group == null) return [member];

            var squad = member.Group.GetEntitiesInGroup<ISkirmishParticipant>().Where(CanFight).ToList();
            if (!squad.Contains(member)) squad.Add(member);
            return squad;
        }

        private void OnRoundResolved(NpcSkirmish skirmish, SkirmishRound round)
        {
            var roundWinners = round.SideAWon ? skirmish.SideA : skirmish.SideB;
            var roundLosers = round.SideAWon ? skirmish.SideB : skirmish.SideA;
            gameEventBus.Publish(new NpcSkirmishRoundResolvedEvent(
                roundWinners[0].Position, round.Number, Ids(roundWinners), Ids(roundLosers)));
        }

        private void OnCompleted(NpcSkirmish skirmish)
        {
            _active.Remove(skirmish);

            foreach (var winner in skirmish.Winners)
                winner.IsFighting = false;

            bool winnersAreLiving = skirmish.Winners.Any(winner => winner.Fraction != Fractions.Undead);
            foreach (var loser in skirmish.Losers)
            {
                loser.IsFighting = false;
                loser.DefeatInWorld();
                // The living burn undead bodies after a victory — deliberately not always.
                if (winnersAreLiving && loser.Fraction == Fractions.Undead && _rnd.RandFloat() < _config.UndeadBurnChance)
                    loser.TryBurnBody();
            }

            var position = skirmish.Losers.Count > 0 ? skirmish.Losers[0].Position : skirmish.Winners[0].Position;
            gameEventBus.Publish(new NpcSkirmishEndedEvent(position, Ids(skirmish.Winners), Ids(skirmish.Losers)));
        }

        private static List<string> Ids(IReadOnlyList<ISkirmishParticipant> side) =>
            side.Select(participant => participant.InstanceId).ToList();
    }
}
