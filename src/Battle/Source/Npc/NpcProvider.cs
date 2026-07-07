namespace Battle.Source.Npc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Ai;
    using Core.Ai.World;
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads Npc.json + NpcBehavior.json and rolls ready-to-apply <see cref="NpcDefinition"/>s:
    /// stance from the allowed set (the behavior archetype follows it), level within the
    /// EntityType cap, rarity by weight, a weighted ability pick from the archetype pool.
    /// </summary>
    public class NpcProvider : INpcProvider
    {
        private const string NpcDataPath = "res://Data/Npc/";
        private const string BehaviorDataPath = "res://Data/NpcBehaviors/";

        private static readonly (Rarity Rarity, float Weight)[] s_rarityWeights =
        [
            (Rarity.Uncommon, 40f),
            (Rarity.Rare, 30f),
            (Rarity.Epic, 15f),
            (Rarity.Legendary, 10f),
            (Rarity.Mythic, 5f),
        ];

        private readonly Dictionary<string, NpcData> _npcs = [];
        private readonly Dictionary<Stance, NpcBehaviorData> _behaviors = [];
        private readonly IAbilityProvider _abilityProvider;
        private readonly INpcModifierProvider _modifierProvider;
        private readonly IRandomNumberGenerator _rnd = new DefaultRandomNumberGenerator();

        public NpcProvider(IAbilityProvider abilityProvider, INpcModifierProvider modifierProvider)
        {
            _abilityProvider = abilityProvider;
            _modifierProvider = modifierProvider;
            _ = LoadDataAsync();
        }

        public IReadOnlyCollection<string> KnownNpcIds => _npcs.Keys;

        public NpcDefinition CreateDefinition(string npcId) => CreateDefinition(npcId, null);

        public NpcDefinition CreateDefinition(string npcId, NpcDefinitionOverrides? overrides)
        {
            var data = _npcs.GetValueOrDefault(npcId)
                       ?? throw new KeyNotFoundException($"No NPC data loaded for '{npcId}'");

            var entityType = ParseEnum<EntityType>(data.EntityType);
            var stance = overrides?.Stance ?? RollStance(data);
            var behaviorData = _behaviors.GetValueOrDefault(stance)
                               ?? throw new KeyNotFoundException($"No behavior archetype loaded for stance '{stance}'");
            int level = overrides?.Level ?? _rnd.RandIntRange(data.LevelMin, NpcTypeDefaults.MaxLevel(entityType));
            var rarity = overrides?.Rarity ?? RollRarity();

            return new NpcDefinition
            {
                NpcId = data.Id,
                Level = level,
                Rarity = rarity,
                Modifiers = RollModifiers(entityType, rarity),
                EntityType = entityType,
                Fraction = ParseEnum<Fractions>(data.Fraction),
                Stance = stance,
                Parameters = ScaleParameters(data, level),
                Abilities = PickAbilities(data, behaviorData, entityType),
                Behavior = BuildProfile(behaviorData, ParseEnum<AiIntellect>(data.AiIntellect)),
                World = BuildWorldConfig(data.World),
                Lifecycle = BuildLifecycleConfig(data.Lifecycle),
            };
        }

        private static NpcLifecycleConfig BuildLifecycleConfig(NpcLifecycleData? data) => data == null ? new NpcLifecycleConfig() : new NpcLifecycleConfig
        {
            ResurrectMinSeconds = data.ResurrectMinSeconds,
            ResurrectMaxSeconds = data.ResurrectMaxSeconds,
            MaxStrengthBonus = data.MaxStrengthBonus,
        };

        private static WorldBrainConfig? BuildWorldConfig(NpcWorldData? data) => data == null ? null : new WorldBrainConfig
        {
            VisionRadius = data.VisionRadius,
            HearingRadius = data.HearingRadius,
            LeashRadius = data.LeashRadius,
            MoveSpeed = data.MoveSpeed,
            ChaseSpeedMultiplier = data.ChaseSpeedMultiplier,
            SuspiciousSeconds = data.SuspiciousSeconds,
            SearchSeconds = data.SearchSeconds,
            PostBattleGraceSeconds = data.PostBattleGraceSeconds,
            Aggressive = data.Aggressive,
            HostileToPlayer = data.HostileToPlayer,
            Activity = ParseEnum<WorldActivityType>(data.Activity),
            WanderRadius = data.WanderRadius,
            ActivityPauseSeconds = data.ActivityPauseSeconds,
            Schedule = data.Schedule.Select(BuildScheduleSlot).ToList(),
        };

        private static ScheduleSlotConfig BuildScheduleSlot(NpcScheduleSlotData data) => new(
            ParseMinuteOfDay(data.From),
            ParseMinuteOfDay(data.To),
            ParseEnum<WorldActivityType>(data.Activity),
            data.WanderRadius);

        /// <summary>"HH:MM" → minutes since midnight.</summary>
        private static int ParseMinuteOfDay(string time)
        {
            string[] parts = time.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int hours) || !int.TryParse(parts[1], out int minutes))
                throw new FormatException($"'{time}' is not a valid HH:MM time");

            return Math.Clamp(hours, 0, 23) * 60 + Math.Clamp(minutes, 0, 59);
        }

        private Stance RollStance(NpcData data)
        {
            if (data.Stances.Count == 0) return Stance.Dexterity;
            string rolled = data.Stances[_rnd.RandIntRange(0, data.Stances.Count - 1)];
            return ParseEnum<Stance>(rolled);
        }

        private Rarity RollRarity()
        {
            long index = _rnd.RandWeighted(s_rarityWeights.Select(entry => entry.Weight).ToArray());
            return s_rarityWeights[Math.Max(0, (int)index)].Rarity;
        }

        /// <summary>Weighted pick without replacement from the shared modifier pool; count = type × rarity.</summary>
        private List<INpcModifier> RollModifiers(EntityType entityType, Rarity rarity)
        {
            int count = NpcTypeDefaults.ModifierCount(entityType, rarity);
            var pool = _modifierProvider.GetAllModifiers().ToList();

            List<INpcModifier> rolled = [];
            while (rolled.Count < count && pool.Count > 0)
            {
                long index = _rnd.RandWeighted(pool.Select(modifier => modifier.Weight).ToArray());
                var picked = pool[Math.Max(0, (int)index)];
                pool.Remove(picked);
                rolled.Add(_modifierProvider.GetModifier(picked.Id));
            }

            return rolled;
        }

        private Dictionary<EntityParameter, float> ScaleParameters(NpcData data, int level)
        {
            var parameters = new Dictionary<EntityParameter, float>();
            float levelFactor = 1f + (level - 1) * data.LevelScaling;
            foreach ((string key, float value) in data.BaseParameters)
            {
                var parameter = ParseEnum<EntityParameter>(key);
                parameters[parameter] = NpcTypeDefaults.ScalesWithLevel(parameter) ? value * levelFactor : value;
            }

            return parameters;
        }

        /// <summary>Weighted pick without replacement from the archetype pool, capped by the book's slots upstream.</summary>
        private List<IAbility> PickAbilities(NpcData data, NpcBehaviorData behavior, EntityType entityType)
        {
            int count = data.AbilityCount > 0 ? data.AbilityCount : NpcTypeDefaults.DefaultAbilityCount(entityType);
            var pool = behavior.Abilities.Where(entry => _abilityProvider.KnownAbilityIds.Contains(entry.Id)).ToList();

            List<IAbility> picked = [];
            while (picked.Count < count && pool.Count > 0)
            {
                long index = _rnd.RandWeighted(pool.Select(entry => entry.Weight).ToArray());
                var entry = pool[Math.Max(0, (int)index)];
                pool.Remove(entry);
                picked.Add(_abilityProvider.CreateAbility(entry.Id));
            }

            return picked;
        }

        private static BehaviorProfile BuildProfile(NpcBehaviorData data, AiIntellect intellect) => new()
        {
            Id = data.Id,
            Stance = ParseEnum<Stance>(data.Stance),
            Intellect = intellect,
            MaxCastsPerTurn = data.MaxCastsPerTurn,
            Temperature = data.Temperature,
            Aggression = data.Aggression,
            Caution = data.Caution,
            Greed = data.Greed,
            CastScoreThreshold = data.CastScoreThreshold,
            FleeHealthThreshold = data.FleeHealthThreshold,
            Abilities = data.Abilities.ToDictionary(
                entry => entry.Id,
                entry => new AbilityBehavior(entry.Id, entry.Weight, ParseEnum<AbilityRole>(entry.Role))),
        };

        private static T ParseEnum<T>(string value) where T : struct, Enum =>
            Enum.TryParse(value, ignoreCase: true, out T result)
                ? result
                : throw new FormatException($"'{value}' is not a valid {typeof(T).Name}");

        private async Task LoadDataAsync()
        {
            try
            {
                await DataLoader.LoadDataFromJson(NpcDataPath, ParseNpcs);
                await DataLoader.LoadDataFromJson(BehaviorDataPath, ParseBehaviors);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to load NPC data", e);
            }
        }

        private Task ParseNpcs(string json)
        {
            var root = JsonConvert.DeserializeObject<NpcsData>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC data");
            foreach (var npc in root.Npcs)
                _npcs[npc.Id] = npc;
            return Task.CompletedTask;
        }

        private Task ParseBehaviors(string json)
        {
            var root = JsonConvert.DeserializeObject<NpcBehaviorsData>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC behavior data");
            foreach (var behavior in root.Behaviors)
                _behaviors[ParseEnum<Stance>(behavior.Stance)] = behavior;
            return Task.CompletedTask;
        }
    }
}
