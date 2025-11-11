namespace Crafting.Source
{
    using Godot;
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Mediator;
    using System.Collections.Generic;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Events;

    public class CraftingMastery : ICraftingMastery
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
        private const int BaseExp = 50;
        private const int MaxLevel = 25;
        // ---------------------------------------------------------

        private int _currentLevel = 1, _bonusLevel = 0;

        private readonly RandomNumberGenerator _rnd;

        // TODO: Same
        // ________________________________________________________
        // Level one probabilities
        private readonly float[] _rarityBase = [65f, 24f, 10f, 1f];
        // Level 25 probabilities
        private readonly float[] _rarityTarget = [10f, 30f, 35f, 25f];
        // --------------------------------------------------------


        private readonly IMediator _mediator;

        public int CurrentExp { get; private set; } = 0;

        public int CurrentLevel
        {
            get => _currentLevel + BonusLevel;
            private set => _currentLevel = value;
        }

        public int BonusLevel
        {
            get => _bonusLevel;
            private set
            {
                if (value != _bonusLevel)
                {
                    _bonusLevel = value;
                    BonusLevelChange?.Invoke(_bonusLevel);
                }
            }
        }

        public event Action<int>? ExpirienceChange, BonusLevelChange;

        public CraftingMastery(IMediator mediator, RandomNumberGenerator rnd)
        {
            _mediator = mediator;
            _rnd = rnd;
        }

        public void AddExpirience(int bonusAmount)
        {
            if (bonusAmount <= 0) return;
            CurrentExp += bonusAmount;
            ExpirienceChange?.Invoke(CurrentExp);
            if (_currentLevel >= MaxLevel) return;
            CheckForLevelUp();
        }
        // TODO: Мастери должно влиять так же на шансы получить более редкий модификатор при рекрафте
        // Позднее возможно так же добавить шансы на получение более редких способностей
        // Сюда же шансы на апгрейд предметов

        public int ExpToNextLevelRemain()
        {
            if (_currentLevel >= MaxLevel) return 0;

            return Mathf.Max(0, ExpToNextLevel(_currentLevel) - CurrentExp);
        }

        public float GetFinalSkillChance(float skillBonus = default)
        {
            float progress = GetProgressFactor();
            float baseChance = Mathf.Lerp(BaseSkillChance, TargetSkillChance, progress);
            float finalChance = baseChance * (1f + skillBonus);
            return Mathf.Clamp(finalChance, 0f, 1f);
        }

        public float GetFinalValueMultiplier(float multiplierBonus = default)
        {
            float progress = GetProgressFactor();
            float baseChance = Mathf.Lerp(BaseValueMultiplier, TargetValueMultiplier, progress);
            float chanceWithBonus = baseChance * (1f + multiplierBonus);
            float finalChance = _rnd.RandfRange(chanceWithBonus * 0.9f, chanceWithBonus * 1.1f);
            return Mathf.Min(finalChance, 1.9f);
        }

        public float GetFinalResourceMultiplier(float resourceBonus = default)
        {
            float progress = GetProgressFactor();
            float baseMultiplier = Mathf.Lerp(BaseResourceReturn, TargetResourceReturn, progress);
            float finalMultiplier = baseMultiplier * (1f + resourceBonus);
            return Math.Min(finalMultiplier, 1.5f);
        }

        public Rarity RollRarity(float rarityBonus = default)
        {
            var probs = GetRarityProbabilities(rarityBonus);
            float r = _rnd.Randf();
            float accumulated = 0;

            foreach (var kvp in probs.OrderBy(x => x.Key))
            {
                accumulated += kvp.Value;
                if (r <= accumulated)
                    return kvp.Key;
            }

            return probs.OrderByDescending(x => x.Key).First().Key;
        }

        private void CheckForLevelUp()
        {
            while (_currentLevel < MaxLevel)
            {
                int need = ExpToNextLevel(_currentLevel);
                if (CurrentExp >= need)
                {
                    CurrentExp -= need;
                    _currentLevel++;
                    // EventBus
                   // _mediator.Publish(new SendNotificationMessageEvent($"Crafting Mastery reached lvl: {_currentLevel}"));
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

        public Dictionary<Rarity, float> GetRarityProbabilities(float rarityBonus = default)
        {
            float progress = GetProgressFactor();

            var interpolatedWeights = new float[_rarityBase.Length];
            float sumWeights = 0f;

            for (int i = 0; i < interpolatedWeights.Length; i++)
            {
                interpolatedWeights[i] = Mathf.Lerp(_rarityBase[i], _rarityTarget[i], progress);
                sumWeights += interpolatedWeights[i];
            }

            for (int i = 0; i < interpolatedWeights.Length; i++)
                interpolatedWeights[i] /= sumWeights;

            var finalWeights = ApplyRarityModifiers(interpolatedWeights, rarityBonus);

            var result = new Dictionary<Rarity, float>();
            for (int i = 0; i < finalWeights.Length; i++)
            {
                result[(Rarity)i] = Mathf.Clamp(finalWeights[i], 0f, 1f);
            }
            return result;
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

        private float GetProgressFactor() => Mathf.Clamp((_currentLevel + BonusLevel - 1) / (float)(MaxLevel + BonusLevel - 1), 0f, 1f);
    }
}
