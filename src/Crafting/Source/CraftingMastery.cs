namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>Levels 0..max (data-driven, 50 shipped) linearly lerp six bonus channels from 0 to
    /// their json maximums: sharpening chance, created values, rarer items, extra effect, mythic
    /// modifier, shatter refund. All tuning — curve, exp rewards, weights — lives in the
    /// CraftingMastery catalog; the constructor reads nothing.</summary>
    public class CraftingMastery(IGameMessageBus gameMessageBus, IRandomNumberGenerator rnd) : ICraftingMastery, IGameDataParticipant, Core.Session.ISessionResettable
    {
        private CraftingMasteryData _config = new();
        // Enum-keyed mirrors of the string-keyed config maps, converted strictly on Apply.
        private Dictionary<Rarity, float> _rarityWeights = ParseRarityWeights(new CraftingMasteryData());
        private Dictionary<Rarity, int> _expByRarity = ParseExpByRarity(new CraftingMasteryData());
        private Dictionary<CraftingMode, float> _expModeFactors = ParseExpModeFactors(new CraftingMasteryData());

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

        public int MaximumLevel => _config.MaxLevel;

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

        public IReadOnlyList<string> Catalogs => [DataCatalog.CraftingMastery];

        public void Apply(string catalog, GameDataFile file)
        {
            _config = JsonConvert.DeserializeObject<CraftingMasteryData>(file.Json)
                      ?? throw new InvalidOperationException($"Failed to deserialize crafting mastery config '{file.FileName}'");
            _rarityWeights = ParseRarityWeights(_config);
            _expByRarity = ParseExpByRarity(_config);
            _expModeFactors = ParseExpModeFactors(_config);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            CurrentExperience += amount;
            if (CurrentLevel >= MaximumLevel) return;
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

        public int ExpToNextLevelRemain() => CurrentLevel >= MaximumLevel ? 0 : Mathf.Max(0, ExpToNextLevel(CurrentLevel) - CurrentExperience);

        public int ExpToNextLevelTotal() => CurrentLevel >= MaximumLevel ? 0 : ExpToNextLevel(CurrentLevel);

        // Channel 1: the upgrader multiplies its own level curve by (1 + this).
        public float GetUpgradeChanceBonus() => LerpBonus(_config.Bonuses.UpgradeChance);

        // Channel 2: no low-mastery penalty — level 0 crafts at x1.0 and mastery only adds on top.
        public float GetCurrentValueMultiplier(float multiplierBonus = 0) =>
            (1f + LerpBonus(_config.Bonuses.CreatedValues)) * (1f + multiplierBonus);

        // Channel 4: consumed by item creation — a crafted item may roll a bonus grant.
        public float GetExtraEffectChanceBonus() => LerpBonus(_config.Bonuses.ExtraEffect);
        public float GetExtraEffectChance() => _config.ExtraEffectBaseChance * (1f + GetExtraEffectChanceBonus());
        public float GetMythicModifierChanceBonus() => LerpBonus(_config.Bonuses.MythicModifier);

        // Channel 3, raw: the mastery panel shows the bonus itself next to the reweighted probabilities.
        public float GetRarityChanceBonus() => LerpBonus(_config.Bonuses.RarerItem);

        // Ascension tuning straight from data; the gate counts bonus levels exactly like the channels do.
        public bool IsAscensionUnlocked => CurrentLevel + BonusLevel >= _config.AscensionLevelGate;
        public float AscensionStatBonus => _config.AscensionStatBonus;

        // Channel 5 applied: like channel 6, the base lives in data and mastery folds its bonus in.
        public float GetMythicGiftChance() => _config.MythicGiftBaseChance * (1f + GetMythicModifierChanceBonus());

        // Channel 6: the final refund fraction, base fraction scaled by the level bonus.
        public float GetCurrentResourceMultiplier(float resourceBonus = 0) =>
            _config.BaseResourceReturn * (1f + LerpBonus(_config.Bonuses.ResourceReturn)) * (1f + resourceBonus);

        // Channel 6, raw: consumers that must yield ×1 at zero mastery (shatter dust) apply
        // the lerped bonus themselves instead of the refund fraction.
        public float GetResourceReturnBonus() => LerpBonus(_config.Bonuses.ResourceReturn);

        public int GetExperienceReward(CraftingMode mode, Rarity rarity) =>
            Mathf.RoundToInt(_expByRarity.GetValueOrDefault(rarity) * _expModeFactors.GetValueOrDefault(mode, 1f));

        public Rarity RollRarity(float rarityBonus = 0)
        {
            var rarityProbabilities = GetRarityProbabilities(rarityBonus);
            float rarity = rnd.RandFloat();
            float accumulated = 0;

            foreach (var kvp in rarityProbabilities.OrderBy(x => x.Key))
            {
                accumulated += kvp.Value;
                if (rarity <= accumulated)
                    return kvp.Key;
            }

            return rarityProbabilities.OrderByDescending(x => x.Key).First().Key;
        }

        // Channel 3: ONE weight set from data; the total bonus (mastery level + external channel)
        // reweights it towards the rare end — the rarest entry gets the full bonus, the most common
        // none (quadratic falloff), then everything renormalizes.
        public Dictionary<Rarity, float> GetRarityProbabilities(float rarityBonus = 0)
        {
            float bonus = LerpBonus(_config.Bonuses.RarerItem) + rarityBonus;

            // Rarity enum: lower value = rarer (Legendary=0 ... Common). Order by descending value so
            // index 0 is the most common; the factor grows towards the rare end of the list.
            var ordered = _rarityWeights.OrderByDescending(pair => pair.Key).ToList();
            float sum = 0f;
            var adjusted = new float[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                float rarityFactor = ordered.Count == 1 ? 1f : Mathf.Pow(i / (float)(ordered.Count - 1), 2);
                adjusted[i] = ordered[i].Value * (1f + bonus * rarityFactor);
                sum += adjusted[i];
            }

            var result = new Dictionary<Rarity, float>();
            for (int i = 0; i < ordered.Count; i++)
                result[ordered[i].Key] = sum <= 0f ? 0f : adjusted[i] / sum;

            return result;
        }

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        /// <summary>Strict enum-keyed map conversion: a typo key is reported and dropped, never silently defaulted.</summary>
        private static Dictionary<TEnum, TValue> ParseEnumMap<TEnum, TValue>(Dictionary<string, TValue> source, string context)
            where TEnum : struct, Enum
        {
            var result = new Dictionary<TEnum, TValue>();
            foreach ((string key, var value) in source)
            {
                if (EnumParser.TryParseEnum<TEnum>(key, out var parsed)) result[parsed] = value;
                else Tracker.TrackError($"Skipping crafting mastery {context} entry: '{key}' is not a valid {typeof(TEnum).Name}");
            }

            return result;
        }

        private static Dictionary<Rarity, float> ParseRarityWeights(CraftingMasteryData config) =>
            ParseEnumMap<Rarity, float>(config.RarityWeights, "rarityWeights");

        private static Dictionary<Rarity, int> ParseExpByRarity(CraftingMasteryData config) =>
            ParseEnumMap<Rarity, int>(config.ExpRewards.ByRarity, "expRewards.byRarity");

        private static Dictionary<CraftingMode, float> ParseExpModeFactors(CraftingMasteryData config) =>
            ParseEnumMap<CraftingMode, float>(config.ExpRewards.ModeFactors, "expRewards.modeFactors");

        private void CheckForLevelUp()
        {
            while (CurrentLevel < MaximumLevel)
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
            if (level >= MaximumLevel) return int.MaxValue;
            float value = _config.BaseExp * Mathf.Pow(level, _config.ExpFactor);
            return Mathf.RoundToInt(value);
        }

        // Level progress 0..max lerps a bonus channel 0..maxBonus; equipment bonus levels count in
        // (and clamp at the cap, so over-stacked gear can't push a channel past its maximum).
        private float LerpBonus(float maxBonus) => maxBonus * GetProgressFactor();

        private float GetProgressFactor() =>
            MaximumLevel <= 0 ? 0f : Mathf.Clamp((CurrentLevel + BonusLevel) / (float)MaximumLevel, 0f, 1f);
    }
}
