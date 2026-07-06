namespace Core.Ai.World.Skirmish
{
    using System;
    using System.Collections.Generic;
    using Interfaces.Components;

    /// <summary>One resolved d20 roll of a skirmish. SideAWon decides who plays the attack beat.</summary>
    public record SkirmishRound(int Number, float RollA, float RollB, bool SideAWon);

    /// <summary>
    /// Abstract NPC-vs-NPC combat: best of <see cref="SkirmishConfig.Rounds"/> d20+Strength rolls,
    /// one roll per random 30s–2min pause. The round winner "attacks" (presentation hook);
    /// after the last round the side with fewer round wins loses the skirmish. Pure logic —
    /// the service marks participants, applies deaths and publishes the events.
    /// </summary>
    public class NpcSkirmish
    {
        private readonly IRandomNumberGenerator _rnd;
        private readonly SkirmishConfig _config;
        private readonly float _strengthA;
        private readonly float _strengthB;
        private int _roundsPlayed;
        private int _sideAWins;
        private float _nextRollIn;

        public NpcSkirmish(IReadOnlyList<ISkirmishParticipant> sideA, IReadOnlyList<ISkirmishParticipant> sideB,
            IRandomNumberGenerator rnd, SkirmishConfig? config = null)
        {
            SideA = sideA;
            SideB = sideB;
            _rnd = rnd;
            _config = config ?? new SkirmishConfig();
            _strengthA = SquadStrength.Calculate(sideA);
            _strengthB = SquadStrength.Calculate(sideB);
            ScheduleNextRoll();
        }

        public IReadOnlyList<ISkirmishParticipant> SideA { get; }
        public IReadOnlyList<ISkirmishParticipant> SideB { get; }
        public bool IsCompleted { get; private set; }
        public IReadOnlyList<ISkirmishParticipant> Winners { get; private set; } = [];
        public IReadOnlyList<ISkirmishParticipant> Losers { get; private set; } = [];

        public event Action<SkirmishRound>? RoundResolved;
        public event Action<NpcSkirmish>? Completed;

        public void Tick(float delta)
        {
            if (IsCompleted) return;

            _nextRollIn -= delta;
            if (_nextRollIn > 0) return;

            ResolveRound();
            if (_roundsPlayed >= _config.Rounds) Complete();
            else ScheduleNextRoll();
        }

        private void ResolveRound()
        {
            _roundsPlayed++;
            float rollA = _rnd.RandIntRange(1, 20) + _strengthA;
            float rollB = _rnd.RandIntRange(1, 20) + _strengthB;
            bool sideAWon = SideAWonRoll(rollA, rollB);
            if (sideAWon) _sideAWins++;

            RoundResolved?.Invoke(new SkirmishRound(_roundsPlayed, rollA, rollB, sideAWon));
        }

        /// <summary>Ties break by raw strength, then by a coin flip — a round always has a winner.</summary>
        private bool SideAWonRoll(float rollA, float rollB)
        {
            if (Math.Abs(rollA - rollB) > 0.0001f) return rollA > rollB;
            if (Math.Abs(_strengthA - _strengthB) > 0.0001f) return _strengthA > _strengthB;
            return _rnd.RandFloat() < 0.5f;
        }

        private void Complete()
        {
            IsCompleted = true;
            bool sideAWon = _sideAWins * 2 > _config.Rounds;
            Winners = sideAWon ? SideA : SideB;
            Losers = sideAWon ? SideB : SideA;
            Completed?.Invoke(this);
        }

        private void ScheduleNextRoll() =>
            _nextRollIn = _rnd.RandFloatRange(_config.MinRollIntervalSeconds, _config.MaxRollIntervalSeconds);
    }
}
