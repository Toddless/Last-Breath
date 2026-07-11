namespace Battle.Source
{
    using System;
    using Core.Battle;
    using Core.Events;
    using Core.Localization;
    using Core.MessageBus;
    using Godot;

    public class MartialArtMastery(IGameMessageBus bus) : IMartialArtMastery, Core.Session.ISessionResettable
    {
        private const float ExpFactor = 1.8f;
        private const int BaseExp = 50;
        private const int MaxLevel = 50;

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

        public int CurrentLevel
        {
            get => field + BonusLevel;
            private set
            {
                if (value == field) return;
                field = value;
                CurrentLevelChange?.Invoke(field);
            }
        } = 1;

        public int MaximumLevel => MaxLevel;

        public string Description => Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localize(Id);

        public event Action<int>? BonusLevelChange, CurrentLevelChange, ExperienceChange;

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag);

        public void AddExperience(int experience)
        {
            // TODO: Calculate exp up to max level (need for progress bar)
            if (experience <= 0) return;
            CurrentExperience += experience;
            if (CurrentLevel >= MaxLevel) return;
            CheckForLevel();
        }

        public void RestoreState(int baseLevel, int experience)
        {
            CurrentLevel = Mathf.Clamp(baseLevel, 1, MaxLevel);
            CurrentExperience = Mathf.Max(0, experience);
        }

        /// <summary>Unlike RestoreState, the bonus levels zero too: the fresh session has no equipment granting them.</summary>
        public void ResetSession()
        {
            BonusLevel = 0;
            RestoreState(1, 0);
        }

        public void AddBonusLevel() => BonusLevel++;

        public void RemoveBonusLevel() => BonusLevel--;

        public int ExpToNextLevelRemain() => CurrentLevel >= MaxLevel ? 0 : Mathf.Max(0, ExpToNextLevel(CurrentLevel) - CurrentExperience);

        public int ExpToNextLevelTotal() => CurrentLevel >= MaxLevel ? 0 : ExpToNextLevel(CurrentLevel);

        private float GetProgressFactor() =>
            Mathf.Clamp((CurrentLevel + BonusLevel - 1) / ((float)MaxLevel + BonusLevel - 1), 0f, 1f) + 1;

        // Same piece of code like within Crafting mastery class.
        // I don't want to couple two projects via a Core library to reduce code duplication
        private void CheckForLevel()
        {
            while (CurrentLevel < MaxLevel)
            {
                int need = ExpToNextLevel(CurrentLevel);
                if (CurrentExperience >= need)
                {
                    CurrentExperience -= need;
                    CurrentLevel++;
                    bus.PublishMessageAsync(new SendNotificationMessageMessage("Notification_Martial_Art_Mastery_Level_Up"));
                }
                else break;
            }
        }

        private int ExpToNextLevel(int level)
        {
            if (level < 1) level = 1;
            if (level >= MaxLevel) return int.MaxValue;
            float value = BaseExp * Mathf.Pow(level, ExpFactor);
            return Mathf.RoundToInt(value);
        }
    }
}
