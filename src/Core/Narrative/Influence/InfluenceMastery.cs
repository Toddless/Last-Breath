namespace Core.Narrative.Influence
{
    using System;
    using System.Collections.Generic;
    using Data.GameData;
    using Data.InfluenceData;
    using Events;
    using Godot;
    using MessageBus;
    using MessageBus.Messages;
    using Newtonsoft.Json;

    /// <summary>
    /// Leveling mirrors MartialArtMastery/CraftingMastery; the curve constants come from
    /// InfluenceMastery.json instead of code. Exp sources: quest turn-ins (main), passed speech
    /// checks and first conversations (small) — wired by the quest/dialogue systems via AddExperience.
    /// </summary>
    public class InfluenceMastery(IGameMessageBus bus) : IInfluenceMastery, IGameDataParticipant, Session.ISessionResettable
    {
        private InfluenceMasteryData _config = new();

        public string Id => "Mastery_Influence";
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
        }

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

        public int MaximumLevel => _config.MaxLevel;

        public int SpeechCheckExp => _config.SpeechCheckExp;

        public int FirstTalkExp => _config.FirstTalkExp;

        public string Description => Localization.Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localization.Localize(Id);

        public event Action<int>? BonusLevelChange, CurrentLevelChange, ExperienceChange;

        public IReadOnlyList<string> Catalogs => [DataCatalog.Influence];

        public void Apply(string catalog, GameDataFile file) =>
            _config = JsonConvert.DeserializeObject<InfluenceMasteryData>(file.Json)
                      ?? throw new InvalidOperationException("Failed to deserialize influence mastery config");

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool HasTag(string tag) => Tags.Contains(tag);

        public float GetSpeechCheckChance(int difficulty, float bonus = 0)
        {
            var curve = _config.SpeechCheck;
            return Mathf.Clamp(curve.BaseChance + (CurrentLevel - difficulty) * curve.ChancePerLevel + bonus, curve.Min, curve.Max);
        }

        public float GetQuestOfferChance(int tier, float bonus = 0)
        {
            var curve = _config.QuestOffer;
            int difficulty = Math.Max(0, tier - 1) * curve.LevelsPerTier;
            return Mathf.Clamp(curve.BaseChance + (CurrentLevel - difficulty) * curve.ChancePerLevel + bonus, curve.Min, curve.Max);
        }

        public void AddExperience(int experience)
        {
            if (experience <= 0) return;
            CurrentExperience += experience;
            if (CurrentLevel >= MaximumLevel) return;
            CheckForLevel();
        }

        public void RestoreState(int baseLevel, int experience)
        {
            CurrentLevel = Mathf.Clamp(baseLevel, 1, MaximumLevel);
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

        public int ExpToNextLevelRemain() => CurrentLevel >= MaximumLevel ? 0 : Mathf.Max(0, ExpToNextLevel(CurrentLevel) - CurrentExperience);

        public int ExpToNextLevelTotal() => CurrentLevel >= MaximumLevel ? 0 : ExpToNextLevel(CurrentLevel);

        private void CheckForLevel()
        {
            while (CurrentLevel < MaximumLevel)
            {
                int need = ExpToNextLevel(CurrentLevel);
                if (CurrentExperience < need) break;

                CurrentExperience -= need;
                CurrentLevel++;
                bus.PublishMessageAsync(new SendNotificationMessageMessage("Notification_Influence_Mastery_Level_Up"));
            }
        }

        private int ExpToNextLevel(int level)
        {
            if (level < 1) level = 1;
            if (level >= MaximumLevel) return int.MaxValue;
            return Mathf.RoundToInt(_config.BaseExp * Mathf.Pow(level, _config.ExpFactor));
        }
    }
}
