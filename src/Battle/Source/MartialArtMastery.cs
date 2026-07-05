namespace Battle.Source
{
    using System;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;
    using Godot;
    using Utilities;

    public class MartialArtMastery(IGameMessageBus bus) : IMartialArtMastery
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
                    GD.Print($"Notification_Martial_Art_Mastery_Level_Up {CurrentLevel}");
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
