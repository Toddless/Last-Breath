namespace Battle.Internal.Npc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Ai;
    using Core.Ai.World;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads Npc.json + NpcBehavior.json and rolls ready-to-apply <see cref="NpcDefinition"/>s:
    /// stance from the allowed set (the behavior archetype follows it), level within the
    /// EntityType cap, rarity by weight, a weighted ability pick from the archetype pool.
    /// </summary>
    public class NpcProvider : INpcProvider, IGameDataParticipant
    {
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
        }

        public IReadOnlyList<string> Catalogs => [DataCatalog.Npc, DataCatalog.NpcBehaviors];

        public IReadOnlyCollection<string> KnownNpcIds => _npcs.Keys;

        public void Apply(string catalog, GameDataFile file)
        {
            if (catalog == DataCatalog.Npc) ParseNpcs(file.Json);
            else ParseBehaviors(file.Json);
        }

        public NpcDefinition CreateDefinition(string npcId) => CreateDefinition(npcId, null);

        public NpcDefinition CreateDefinition(string npcId, NpcDefinitionOverrides? overrides)
        {
            var data = _npcs.GetValueOrDefault(npcId)
                       ?? throw new KeyNotFoundException($"No NPC data loaded for '{npcId}'");

            var entityType = EnumParser.ParseEnum<EntityType>(data.EntityType);
            var stance = overrides?.Stance ?? RollStance(data);
            var behaviorData = _behaviors.GetValueOrDefault(stance)
                               ?? throw new KeyNotFoundException($"No behavior archetype loaded for stance '{stance}'");
            int level = overrides?.Level ?? _rnd.RandIntRange(data.LevelMin, data.LevelMax ?? NpcTypeDefaults.MaxLevel(entityType));
            // Authored rarity (bosses/uniques) beats the weighted roll; explicit overrides beat both.
            var rarity = overrides?.Rarity ?? ParseFixedRarity(data) ?? RollRarity();
            var stages = NpcStageParser.Parse(data.Id, data.Stages);

            return new NpcDefinition
            {
                NpcId = data.Id,
                Level = level,
                Rarity = rarity,
                Modifiers = RollModifiers(entityType, rarity),
                EntityType = entityType,
                Fraction = EnumParser.ParseEnum<Fractions>(data.Fraction),
                Stance = stance,
                Parameters = ScaleParameters(data, level),
                // Staged bosses learn per-stage sets via ApplyStage — a rolled/authored list would be discarded.
                Abilities = stages.Count > 0 ? [] : PickAbilities(data, behaviorData, entityType),
                Behavior = BuildProfile(behaviorData, EnumParser.ParseEnum<AiIntellect>(data.AiIntellect), data.FleeHealthThreshold, data.AbilityBehaviors),
                World = BuildWorldConfig(data.World),
                Lifecycle = BuildLifecycleConfig(data.Lifecycle),
                CanTalk = data.Interaction?.CanTalk ?? false,
                Reactions = NpcReactionParser.Parse(data.Id, data.Reactions),
                Stages = stages,
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
            Activity = EnumParser.ParseEnum<WorldActivityType>(data.Activity),
            WanderRadius = data.WanderRadius,
            ActivityPauseSeconds = data.ActivityPauseSeconds,
            SleepVisionMultiplier = data.SleepVisionMultiplier,
            SleepHearingMultiplier = data.SleepHearingMultiplier,
            Schedule = data.Schedule.Select(BuildScheduleSlot).ToList(),
            Routine = data.Routine.Select(BuildRoutineStep).ToList(),
        };

        private static ScheduleSlotConfig BuildScheduleSlot(NpcScheduleSlotData data) => new(
            ParseMinuteOfDay(data.From),
            ParseMinuteOfDay(data.To),
            EnumParser.ParseEnum<WorldActivityType>(data.Activity),
            data.WanderRadius,
            data.Point);

        private static RoutineStepConfig BuildRoutineStep(NpcRoutineStepData data) => new(
            EnumParser.ParseEnum<WorldActivityType>(data.Activity),
            data.Minutes,
            data.WanderRadius,
            data.Point);

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
            return EnumParser.ParseEnum<Stance>(rolled);
        }

        /// <summary>Fixed rarity from the data ("rarity" field, bosses/uniques); absent = roll by weight.</summary>
        private static Rarity? ParseFixedRarity(NpcData data) =>
            string.IsNullOrEmpty(data.Rarity) ? null : EnumParser.ParseEnum<Rarity>(data.Rarity);

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
                var parameter = EnumParser.ParseEnum<EntityParameter>(key);
                parameters[parameter] = NpcTypeDefaults.ScalesWithLevel(parameter) ? value * levelFactor : value;
            }

            return parameters;
        }

        /// <summary>Authored list ("abilities" field, bosses) is exact and deterministic; otherwise a
        /// weighted pick without replacement from the archetype pool, capped by the book's slots upstream.</summary>
        private List<IAbility> PickAbilities(NpcData data, NpcBehaviorData behavior, EntityType entityType)
        {
            if (data.Abilities.Count > 0) return CreateAuthoredAbilities(data);

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

        /// <summary>Unknown ids are reported and skipped — a typo must not abort the whole spawn.</summary>
        private List<IAbility> CreateAuthoredAbilities(NpcData data)
        {
            List<IAbility> abilities = [];
            foreach (string abilityId in data.Abilities)
            {
                if (!_abilityProvider.KnownAbilityIds.Contains(abilityId))
                {
                    Core.Tracker.TrackNotFound($"Authored ability '{abilityId}' of npc '{data.Id}'", this);
                    continue;
                }

                abilities.Add(_abilityProvider.CreateAbility(abilityId));
            }

            return abilities;
        }

        private static BehaviorProfile BuildProfile(
            NpcBehaviorData data, AiIntellect intellect, float? fleeOverride, List<NpcAbilityBehaviorData> npcEntries) => new()
        {
            Id = data.Id,
            Stance = EnumParser.ParseEnum<Stance>(data.Stance),
            Intellect = intellect,
            MaxCastsPerTurn = data.MaxCastsPerTurn,
            Temperature = data.Temperature,
            Aggression = data.Aggression,
            Caution = data.Caution,
            Greed = data.Greed,
            CastScoreThreshold = data.CastScoreThreshold,
            // Per-NPC override beats the stance archetype: bosses author 0 (they never flee).
            FleeHealthThreshold = fleeOverride ?? data.FleeHealthThreshold,
            Abilities = MergeAbilityBehaviors(data, npcEntries),
        };

        /// <summary>Per-NPC planner entries win over the archetype's: authored kit abilities that
        /// live outside the stance pool (summons and future boss-only casts) get scored too —
        /// without an entry the planner never picks the ability up.</summary>
        private static Dictionary<string, AbilityBehavior> MergeAbilityBehaviors(NpcBehaviorData data, List<NpcAbilityBehaviorData> npcEntries)
        {
            var merged = data.Abilities.ToDictionary(
                entry => entry.Id,
                entry => new AbilityBehavior(entry.Id, entry.Weight, EnumParser.ParseEnum<AbilityRole>(entry.Role)));
            foreach (var entry in npcEntries)
                merged[entry.Id] = new AbilityBehavior(entry.Id, entry.Weight, EnumParser.ParseEnum<AbilityRole>(entry.Role));
            return merged;
        }

        private void ParseNpcs(string json)
        {
            var root = JsonConvert.DeserializeObject<NpcsData>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC data");
            foreach (var npc in root.Npcs)
                _npcs[npc.Id] = npc;
        }

        private void ParseBehaviors(string json)
        {
            var root = JsonConvert.DeserializeObject<NpcBehaviorsData>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC behavior data");
            foreach (var behavior in root.Behaviors)
                _behaviors[EnumParser.ParseEnum<Stance>(behavior.Stance)] = behavior;
        }
    }
}
