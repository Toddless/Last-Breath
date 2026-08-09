namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Core;
    using Core.Battle;
    using Core.Data.GameData;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.PassiveTree.Allocation;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    /// Martial art mastery. Levels are counted from ZERO, so the fifty levels of the curve are fifty
    /// level ups — and fifty passive tree points, one per level. Quests pay points on top of that
    /// (<see cref="BonusPoints"/>), and the sum is the whole budget the tree is ever told about.
    /// Curve tuning lives in the MartialArtMastery catalog; the constructor reads nothing.
    /// The passive tree is optional: only the game project owns an allocation, so the battle sandbox
    /// resolves null and the points go nowhere.
    /// </summary>
    public class MartialArtMastery(IGameMessageBus bus, Func<IPassiveTreeService?>? passiveTree = null)
        : IMartialArtMastery, IGameDataParticipant, Core.Session.ISessionResettable
    {
        private MartialArtMasteryData _config = new();

        public string Id => "Mastery_Martial_Art";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = [];
        public Texture2D? Icon { get; }

        public int BonusLevel
        {
            get;
            private set
            {
                if (field == value) return;
                field = value;
                BonusLevelChange?.Invoke(field);
            }
        } = 0;

        public int CurrentExperience
        {
            get;
            private set
            {
                if (value == field) return;
                field = value;
                ExperienceChange?.Invoke(field);
            }
        }

        /// <summary>Levels earned by experience: the only number the save keeps, the only one the tree
        /// budget is counted from, and the only one a price scaled by progress may read. Equipment levels
        /// are added on top by <see cref="CurrentLevel"/> and never earned into this one.</summary>
        public int EarnedLevel
        {
            get;
            private set
            {
                if (value == field) return;
                field = value;
                GrantPassivePoints();
                CurrentLevelChange?.Invoke(CurrentLevel);
            }
        }

        /// <summary>Points handed out beside the curve — quest rewards. Restating the tree total on
        /// every change is what makes a grant reach the wheel the same way a level up does.</summary>
        public int BonusPoints
        {
            get;
            private set
            {
                if (value == field) return;
                field = value;
                GrantPassivePoints();
            }
        }

        public int CurrentLevel => EarnedLevel + BonusLevel;

        public int TotalPoints => EarnedLevel + BonusPoints;

        public int MaximumLevel => _config.MaxLevel;

        public string Description => Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localize(Id);

        public IReadOnlyList<string> Catalogs => [DataCatalog.MartialArtMastery];

        public event Action<int>? BonusLevelChange, CurrentLevelChange, ExperienceChange;

        public void Apply(string catalog, GameDataFile file)
        {
            var parsed = JsonConvert.DeserializeObject<MartialArtMasteryData>(file.Json)
                         ?? throw new InvalidOperationException($"Failed to deserialize martial art mastery config '{file.FileName}'");
            _config = Sanitized(parsed, file.FileName);
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag);

        public void AddExperience(int experience)
        {
            // TODO: Calculate exp up to max level (need for progress bar)
            if (experience <= 0) return;
            CurrentExperience += experience;
            if (CurrentLevel >= MaximumLevel) return;
            CheckForLevel();
        }

        /// <summary>Grants tree points a quest paid for. Not idempotent by design — the reward is
        /// kept to one payment by the quest being unrepeatable. A grant that is not a reward is
        /// reported instead of quietly resized: it would move a budget the player already spent.</summary>
        public void AddBonusPoints(int points)
        {
            if (points <= 0)
            {
                Tracker.TrackError($"Martial art mastery: a grant of {points} passive tree points is not a reward");
                return;
            }

            BonusPoints += points;
        }

        public void RestoreState(int baseLevel, int experience, int bonusPoints)
        {
            EarnedLevel = Mathf.Clamp(baseLevel, 0, MaximumLevel);
            CurrentExperience = Mathf.Max(0, experience);
            BonusPoints = Mathf.Max(0, bonusPoints);
        }

        /// <summary>Unlike RestoreState, the bonus levels zero too: the fresh session has no equipment granting them.</summary>
        public void ResetSession()
        {
            BonusLevel = 0;
            RestoreState(0, 0, 0);
        }

        public void AddBonusLevel() => BonusLevel++;

        public void RemoveBonusLevel() => BonusLevel--;

        public int ExpToNextLevelRemain() => CurrentLevel >= MaximumLevel ? 0 : Mathf.Max(0, ExpToNextLevel(CurrentLevel) - CurrentExperience);

        public int ExpToNextLevelTotal() => CurrentLevel >= MaximumLevel ? 0 : ExpToNextLevel(CurrentLevel);

        /// <summary>A curve number that is not positive is reported and replaced by the shipped default:
        /// a zero cap or a zero cost would hand out every level — and every tree point — at once.</summary>
        private static MartialArtMasteryData Sanitized(MartialArtMasteryData parsed, string fileName)
        {
            var defaults = new MartialArtMasteryData();
            return parsed with
            {
                MaxLevel = Positive(parsed.MaxLevel, defaults.MaxLevel, "maxLevel", fileName),
                BaseExp = Positive(parsed.BaseExp, defaults.BaseExp, "baseExp", fileName),
                ExpFactor = Positive(parsed.ExpFactor, defaults.ExpFactor, "expFactor", fileName),
            };
        }

        private static T Positive<T>(T value, T fallback, string field, string fileName) where T : INumber<T>
        {
            if (value > T.Zero) return value;

            Tracker.TrackError($"Martial art mastery '{fileName}': '{field}' must be positive but is {value}, falling back to {fallback}");
            return fallback;
        }

        private float GetProgressFactor() =>
            Mathf.Clamp((CurrentLevel + BonusLevel - 1) / ((float)MaximumLevel + BonusLevel - 1), 0f, 1f) + 1;

        /// <summary>One tree point per earned level, plus whatever was granted beside the curve. The
        /// total is STATED, never added to: a save applied twice restates the same budget instead of
        /// duplicating it.</summary>
        private void GrantPassivePoints() => passiveTree?.Invoke()?.SetTotalPoints(TotalPoints);

        // Same piece of code like within Crafting mastery class.
        // I don't want to couple two projects via a Core library to reduce code duplication
        private void CheckForLevel()
        {
            while (CurrentLevel < MaximumLevel)
            {
                int need = ExpToNextLevel(CurrentLevel);
                if (CurrentExperience < need) break;

                CurrentExperience -= need;
                EarnedLevel++; // the earned base, never the effective level: ++ on the sum would inflate the save
                bus.PublishMessageAsync(new SendNotificationMessageMessage("Notification_Martial_Art_Mastery_Level_Up"));
            }
        }

        /// <summary>Cost of the level being BOUGHT: the curve is indexed by the target level, so the
        /// first level up (0 -> 1) costs baseExp instead of baseExp x 0^factor = nothing. The old
        /// "raise the argument to 1" guard is gone together with the level-1 start it served — on a
        /// zero-based scale it charged the same price for the first two levels.</summary>
        private int ExpToNextLevel(int level)
        {
            if (level >= MaximumLevel) return int.MaxValue;

            // Unbalanced RemoveBonusLevel can push the effective level below zero; the first level still costs.
            int target = Mathf.Max(1, level + 1);
            return Mathf.RoundToInt(_config.BaseExp * Mathf.Pow(target, _config.ExpFactor));
        }
    }
}
