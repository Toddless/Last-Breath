namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Events;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Mediator;
    using System.Collections.Generic;

    public class CraftingMastery(IMediator mediator, RandomNumberGenerator rnd) : ICraftingMastery
    {
        // TODO: Remove from here
        // ________________________________________________________
        private const float BaseSkillChance = 0.15f;
        private const float TargetSkillChance = 0.7f;
        private const float BaseResourceReturn = 0.3f;
        private const float TargetResourceReturn = 1f;
        private const float BaseValueMultiplier = 0.8f;
        private const float TargetValueMultiplier = 1.25f;
        private const float ExpFactor = 1.8f;
        private const float MaxLevel = 25;
        private const int BaseExp = 50;
        // ---------------------------------------------------------

        private int _currentLevel = 1;

        // TODO: Same
        // ________________________________________________________
        // Level one probabilities
        private readonly float[] _rarityBase = [65f, 24f, 10f, 1f];

        // Level 25 probabilities
        private readonly float[] _rarityTarget = [10f, 30f, 35f, 25f];
        // --------------------------------------------------------

        public string Id { get; } = "Mastery_Crafting";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = [];
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public Texture2D? Icon { get; }
        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);

        public int CurrentExperience { get; private set; } = 0;

        public int CurrentLevel
        {
            get => _currentLevel + BonusLevel;
            private set => _currentLevel = value;
        }

        public int BonusLevel
        {
            get => field;
            private set
            {
                if (value == field) return;

                field = value;
                BonusLevelChange?.Invoke(field);
            }
        }

        public event Action<int>? ExperienceChange, BonusLevelChange, CurrentLevelChange;

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            CurrentExperience += amount;
            ExperienceChange?.Invoke(CurrentExperience);
            if (_currentLevel >= MaxLevel) return;
            CheckForLevelUp();
        }
        // TODO: Мастери должно влиять так же на шансы получить более редкий модификатор при рекрафте
        // Позднее возможно так же добавить шансы на получение более редких способностей
        // Сюда же шансы на апгрейд предметов

        public int ExpToNextLevelRemain()
        {
            return _currentLevel >= MaxLevel ? 0 : Mathf.Max(0, ExpToNextLevel(_currentLevel) - CurrentExperience);
        }

        public float GetSkillChance(float skillBonus = default)
        {
            float baseChance = CalculateBase(BaseSkillChance, TargetSkillChance, GetProgressFactor());
            float finalChance = baseChance * (1f + skillBonus);
            return Mathf.Clamp(finalChance, 0f, 1f);
        }

        public float GetValueMultiplier(float multiplierBonus = default)
        {
            float baseChance = CalculateBase(BaseValueMultiplier, TargetValueMultiplier, GetProgressFactor());
            float chanceWithBonus = baseChance * (1f + multiplierBonus);
            float finalChance = rnd.RandfRange(chanceWithBonus * 0.9f, chanceWithBonus * 1.1f);
            return Mathf.Min(finalChance, 1.9f);
        }

        public float GetResourceMultiplier(float resourceBonus = default)
        {
            float baseMultiplier = CalculateBase(BaseResourceReturn, TargetResourceReturn, GetProgressFactor());
            float finalMultiplier = baseMultiplier * (1f + resourceBonus);
            return Math.Min(finalMultiplier, 1.5f);
        }

        public Rarity RollRarity(float rarityBonus = default)
        {
            var probs = GetRarityProbabilities(rarityBonus);
            float r = rnd.Randf();
            float accumulated = 0;

            foreach (var kvp in probs.OrderBy(x => x.Key))
            {
                accumulated += kvp.Value;
                if (r <= accumulated)
                    return kvp.Key;
            }

            return probs.OrderByDescending(x => x.Key).First().Key;
        }

        public Dictionary<Rarity, float> GetRarityProbabilities(float rarityBonus = default)
        {
            float progress = GetProgressFactor();

            float[] interpolatedWeights = new float[_rarityBase.Length];
            float sumWeights = 0f;

            for (int i = 0; i < interpolatedWeights.Length; i++)
            {
                interpolatedWeights[i] = Mathf.Lerp(_rarityBase[i], _rarityTarget[i], progress);
                sumWeights += interpolatedWeights[i];
            }

            for (int i = 0; i < interpolatedWeights.Length; i++)
                interpolatedWeights[i] /= sumWeights;

            float[] finalWeights = ApplyRarityModifiers(interpolatedWeights, rarityBonus);

            var result = new Dictionary<Rarity, float>();
            for (int i = 0; i < finalWeights.Length; i++)
            {
                result[(Rarity)i] = Mathf.Clamp(finalWeights[i], 0f, 1f);
            }

            return result;
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        private float CalculateBase(float baseValue, float targetValue, float progression) => Mathf.Lerp(baseValue, targetValue, progression);

        private void CheckForLevelUp()
        {
            while (_currentLevel < MaxLevel)
            {
                int need = ExpToNextLevel(_currentLevel);
                if (CurrentExperience >= need)
                {
                    CurrentExperience -= need;
                    _currentLevel++;
                    mediator.PublishAsync(new SendNotificationMessageEvent($"Crafting Mastery reached lvl: {_currentLevel}"));
                }
                else
                    break;
            }
        }

        private int ExpToNextLevel(int level)
        {
            if (level < 1) level = 1;
            if (level >= MaxLevel) return int.MaxValue;
            float value = BaseExp * Mathf.Pow(level, ExpFactor);
            return Mathf.RoundToInt(value);
        }

        private float[] ApplyRarityModifiers(float[] baseWeights, float rarityBonus)
        {
            var adjustedWeights = new float[baseWeights.Length];
            float sumAdjusted = 0f;

            for (int i = 0; i < baseWeights.Length; i++)
            {
                float rarityFactor = (float)Math.Pow(i / (float)(baseWeights.Length - 1), 2);
                float bonusMultiplier = 1f + rarityBonus * rarityFactor;

                adjustedWeights[i] = baseWeights[i] * bonusMultiplier;
                sumAdjusted += adjustedWeights[i];
            }

            for (int i = 0; i < adjustedWeights.Length; i++)
                adjustedWeights[i] /= sumAdjusted;

            return adjustedWeights;
        }

        private float GetProgressFactor() =>
            Mathf.Clamp((_currentLevel + BonusLevel - 1) / (MaxLevel + BonusLevel - 1), 0f, 1f);
    }
}
