namespace Battle.Internal.Npc
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Ai.World.Skirmish;
    using Core.Components;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Events.GameEvents;

    /// <summary>
    /// Owns the active skirmishes: expands both sides to their squads, freezes the participants
    /// (IsFighting suspends brains and blocks player battles), republishes round/end events to
    /// the game bus and applies the outcome — losers die into their body lifecycles, undead
    /// losers get burned by living winners with a chance.
    /// </summary>
    internal class NpcSkirmishService(IGameEventBus gameEventBus, IFactionRelationService relations) : INpcSkirmishService
    {
        private readonly List<NpcSkirmish> _active = [];
        private readonly IRandomNumberGenerator _rnd = new DefaultRandomNumberGenerator();
        private readonly SkirmishConfig _config = new();

        public bool TryStart(ISkirmishParticipant initiator, ISkirmishParticipant target)
        {
            if (!CanFight(initiator) || !CanFight(target)) return false;
            // группы сущности сравниваются по InstanceId вместо референса?
            // TODO: Проверить как сохраняются/загружаются группы нпс
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

        private static bool CanFight(ISkirmishParticipant npc) => npc is { IsAlive: true, IsFighting: false };

        /// <summary>The whole group joins the fight (same rule as player encounters with grouped NPCs).</summary>
        private static List<ISkirmishParticipant> ExpandSquad(ISkirmishParticipant member)
        {
            if (member.Group == null) return [member];

            var squad = member.Group.GetEntitiesInGroup<ISkirmishParticipant>().Where(CanFight).ToList();
            if (!squad.Contains(member)) squad.Add(member);
            return squad;
        }

        // TODO:
        // Эвент публикуется, но на текущий момент его никто не слушает. Заготовка под анимации если игрок неподалеку
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
                // TODO: Анимация сожжения тела. Победители поджигают, пораженные горят

                // The living burn undead bodies after a victory — deliberately not always.
                if (winnersAreLiving && loser.Fraction == Fractions.Undead && _rnd.RandFloat() < _config.UndeadBurnChance)
                    loser.TryBurnBody();
            }

            var position = skirmish.Losers.Count > 0 ? skirmish.Losers[0].Position : skirmish.Winners[0].Position;
            //TODO:
            // на текущий момент ноль подписок на данный эвент.
            gameEventBus.Publish(new NpcSkirmishEndedEvent(position, Ids(skirmish.Winners), Ids(skirmish.Losers)));
        }

        private static List<string> Ids(IReadOnlyList<ISkirmishParticipant> side) =>
            side.Select(participant => participant.InstanceId).ToList();
    }
}
