namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Crafting;
    using Core.Enums;
    using Core.Events;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Godot;

    public class CraftingMastery(IGameMessageBus gameMessageBus, RandomNumberGenerator rnd) : ICraftingMastery, Core.Session.ISessionResettable
    {
        // TODO: Remove from here
        // ________________________________________________________
        private const float BaseSkillChance = 0.15f;
        private const float TargetSkillChance = 0.7f;

        private const float BaseResourceReturn = 0.3f;
        private const float TargetResourceReturn = 1f;

        private const float BaseValueMultiplier = 0.2f;
        private const float TargetValueMultiplier = 1.25f;

        private const float BaseMinRange = 0.4f;
        private const float BaseMaxRange = 1.15f;

        private const float TargetMinRange = 0.9f;
        private const float TargetMaxRange = 1.25f;

        private const float ExpFactor = 1.8f;
        private const int MaxLevel = 25;
        private const int BaseExp = 50;
        // ---------------------------------------------------------

        // TODO: Same
        // ________________________________________________________
        // Level one probabilities
        private readonly float[] _rarityBase = [1f, 10f, 24f, 65f];

        // Level 25 probabilities
        private readonly float[] _rarityTarget = [10f, 30f, 35f, 25f];
        // --------------------------------------------------------

        public string Id => "Mastery_Crafting";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = [];
        public Texture2D? Icon { get; }
        public string Description => Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localize(Id);

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
            get;
            private set
            {
                if (value == field) return;
                field = value;
                CurrentLevelChange?.Invoke(field);
            }
        }

        public int MaximumLevel => MaxLevel;

        public int BonusLevel
        {
            get;
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
            if (CurrentLevel >= MaxLevel) return;
            CheckForLevelUp();
        }

        public void AddBonusLevel() => BonusLevel++;

        public void RemoveBonusLevel() => BonusLevel--;

        /// <summary>Back to the fresh-process values (this mastery starts at base level 0); bonus levels zero with the lost equipment.</summary>
        public void ResetSession()
        {
            BonusLevel = 0;
            CurrentLevel = 0;
            CurrentExperience = 0;
        }
        // TODO: Мастери должно влиять так же на шансы получить более редкий модификатор при рекрафте
        // Позднее возможно так же добавить шансы на получение более редких способностей
        // Сюда же шансы на апгрейд предметов

        public int ExpToNextLevelRemain() => CurrentLevel >= MaxLevel ? 0 : Mathf.Max(0, ExpToNextLevel(CurrentLevel) - CurrentExperience);

        public int ExpToNextLevelTotal() => CurrentLevel >= MaxLevel ? 0 : ExpToNextLevel(CurrentLevel);

        public float GetCurrentSkillChance(float skillBonus = 0)
        {
            float baseChance = CalculateBase(BaseSkillChance, TargetSkillChance, GetProgressFactor());
            float finalChance = baseChance * (1f + skillBonus);
            return Mathf.Clamp(finalChance, 0f, 1f);
        }

        public float GetCurrentValueMultiplier(float multiplierBonus = 0)
        {
            float baseChance = CalculateBase(BaseValueMultiplier, TargetValueMultiplier, GetProgressFactor());
            float chanceWithBonus = baseChance * (1f + multiplierBonus);
            return Mathf.Min(chanceWithBonus, 1.9f);
        }

        public float GetCurrentResourceMultiplier(float resourceBonus = 0)
        {
            float baseMultiplier = CalculateBase(BaseResourceReturn, TargetResourceReturn, GetProgressFactor());
            float finalMultiplier = baseMultiplier * (1f + resourceBonus);
            return Math.Min(finalMultiplier, 1.5f);
        }

        public float GetCurrentMinRange() => CalculateBase(BaseMinRange, TargetMinRange, GetProgressFactor());
        public float GetCurrentMaxRange() => CalculateBase(BaseMaxRange, TargetMaxRange, GetProgressFactor());
        public float GetRandomValueRange() => rnd.RandfRange(GetCurrentMinRange(), GetCurrentMaxRange());

        public Rarity RollRarity(float rarityBonus = 0)
        {
            var rarityProbabilities = GetRarityProbabilities(rarityBonus);
            float rarity = rnd.Randf();
            float accumulated = 0;

            foreach (var kvp in rarityProbabilities.OrderBy(x => x.Key))
            {
                accumulated += kvp.Value;
                if (rarity <= accumulated)
                    return kvp.Key;
            }

            return rarityProbabilities.OrderByDescending(x => x.Key).First().Key;
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

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        private float CalculateBase(float baseValue, float targetValue, float progression) => Mathf.Lerp(baseValue, targetValue, progression);

        private void CheckForLevelUp()
        {
            while (CurrentLevel < MaxLevel)
            {
                int need = ExpToNextLevel(CurrentLevel);
                if (CurrentExperience >= need)
                {
                    CurrentExperience -= need;
                    CurrentLevel++;
                    gameMessageBus.PublishMessageAsync(new SendNotificationMessageMessage("Notification_Crafting_Mastery_Level_Up"));
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
            Mathf.Clamp((float)(CurrentLevel + BonusLevel - 1) / (MaxLevel + BonusLevel - 1), 0f, 1f);
    }
}
