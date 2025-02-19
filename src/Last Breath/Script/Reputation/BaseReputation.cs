namespace Playground.Script.Reputation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.Enums;

    public abstract class BaseReputation(int defaultReputation)
    {
        private int _currentReputationPoints = defaultReputation;
        private ReputationRank _currentRank;
        private readonly Dictionary<Func<int, bool>, ReputationRank> _reputationRanks = new()
        {
        { v => v <= -3000, ReputationRank.Hate },
        { v => v > -3000 && v <= -2000, ReputationRank.Feud },
        { v => v > -2000 && v <= -1000, ReputationRank.Detestation },
        { v => v > -1000 && v <= 1000, ReputationRank.Neutrality },
        { v => v > 1000 && v <= 2000, ReputationRank.Friendliness },
        { v => v > 2000 && v <= 3000, ReputationRank.Reverence},
        { v => v > 3000, ReputationRank.Allyship }
        };

        public int CurrentReputationPoints
        {
            get => _currentReputationPoints;
            private set => _currentReputationPoints = value;
        }

        public ReputationRank CurrentRank
        {
            get => _currentRank;
            private set => _currentRank = value;
        }

        public void ChangeReputationPoints(int amount)
        {
            _currentReputationPoints += amount;
            var newRank = _reputationRanks.First(pair => pair.Key(_currentReputationPoints)).Value;
            if (_currentRank != newRank)
                _currentRank = newRank;
        }
    }
}
