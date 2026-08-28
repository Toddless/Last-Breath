namespace Core.Entity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ai;
    using Ai.World;
    using Battle.Abilities;
    using Components;
    using Data;
    using Data.GameData;
    using Data.NpcData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Shared NPC provider: loads the Npc and NpcBehaviors catalogs and rolls ready-to-apply
    /// <see cref="NpcDefinition"/>s — stance from the allowed set (the behavior archetype follows
    /// it), level within the EntityType cap, rarity by weight, a weighted ability pick from the
    /// archetype pool. Every project spawning NPCs binds <see cref="INpcProvider"/> to this class.
    /// A record carrying an "authored" section (<see cref="NpcAuthoredData"/>) replaces the rolls it
    /// names with the values the data states — that is how a named villager is placed by hand.
    /// <para>
    /// HOW MANY modifiers and abilities come out is a roll of its own: the type × rarity tables give
    /// the number of SLOTS, and <see cref="INpcSpawnRollsProvider"/> gives each slot its falling
    /// chance of being filled (see <see cref="RollModifiers"/>). <paramref name="rollSeed"/> exists so
    /// a test can hold that distribution still; nothing in the game passes it.
    /// </para>
    /// </summary>
    public class NpcProvider(
        IAbilityProvider abilityProvider,
        INpcModifierProvider modifierProvider,
        INpcSpawnRollsProvider spawnRolls,
        int? rollSeed = null) : INpcProvider, IGameDataParticipant
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
        private readonly IRandomNumberGenerator _rnd = new DefaultRandomNumberGenerator(rollSeed);

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
            // Authored facts (named villagers, trial targets) are placed by hand and never rolled:
            // two spawns of the id must hand out one fighter. Runtime overrides still beat them —
            // the save system rebuilding a body knows better than the catalog what that body was.
            var stance = overrides?.Stance ?? ParseAuthoredStance(data.Authored) ?? RollStance(data);
            var behaviorData = _behaviors.GetValueOrDefault(stance)
                               ?? throw new KeyNotFoundException($"No behavior archetype loaded for stance '{stance}'");
            int level = overrides?.Level ?? data.Authored?.Level
                        ?? _rnd.RandIntRange(data.LevelMin, data.LevelMax ?? NpcTypeDefaults.MaxLevel(entityType));
            // Authored rarity (bosses/uniques) beats the weighted roll; explicit overrides beat both.
            var rarity = overrides?.Rarity ?? ParseAuthoredRarity(data.Authored) ?? ParseFixedRarity(data) ?? RollRarity();
            var stages = NpcStageParser.Parse(data.Id, data.Stages);

            return new NpcDefinition
            {
                NpcId = data.Id,
                Level = level,
                Rarity = rarity,
                Modifiers = RollModifiers(entityType, rarity, data.Authored),
                EntityType = entityType,
                Fraction = EnumParser.ParseEnum<Fractions>(data.Fraction),
                Stance = stance,
                Parameters = ScaleParameters(data, level),
                // Staged bosses learn per-stage sets via ApplyStage — a rolled/authored list would be discarded.
                Abilities = stages.Count > 0 ? [] : PickAbilities(data, behaviorData, entityType, rarity),
                Behavior = BuildProfile(behaviorData, EnumParser.ParseEnum<AiIntellect>(data.AiIntellect), data.FleeHealthThreshold, data.AbilityBehaviors),
                World = BuildWorldConfig(data.World),
                LifecycleKind = ParseLifecycleKind(data.Lifecycle),
                Lifecycle = BuildLifecycleConfig(data.Lifecycle),
                VillagerLifecycle = BuildVillagerLifecycleConfig(data.Lifecycle),
                CanTalk = data.Interaction?.CanTalk ?? false,
                Reactions = NpcReactionParser.Parse(data.Id, data.Reactions),
                Passives = data.Passives,
                Stages = stages,
            };
        }

        /// <summary>Which post-defeat cycle the record chose. No section or no "kind" = the undead
        /// cycle every NPC had before the villager one existed; a typo is refused like any other
        /// enum of the file, so a villager can never quietly become a rising undead.</summary>
        private static NpcLifecycleKind ParseLifecycleKind(NpcLifecycleData? data) =>
            EnumParser.ParseEnumOrDefault<NpcLifecycleKind>(data?.Kind);

        private static NpcLifecycleConfig BuildLifecycleConfig(NpcLifecycleData? data) => data == null ? new NpcLifecycleConfig() : new NpcLifecycleConfig
        {
            ResurrectMinSeconds = data.ResurrectMinSeconds,
            ResurrectMaxSeconds = data.ResurrectMaxSeconds,
            MaxStrengthBonus = data.MaxStrengthBonus,
        };

        private static VillagerLifecycleConfig BuildVillagerLifecycleConfig(NpcLifecycleData? data) => data == null ? new VillagerLifecycleConfig() : new VillagerLifecycleConfig
        {
            RecoverMinSeconds = data.RecoverMinSeconds,
            RecoverMaxSeconds = data.RecoverMaxSeconds,
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

        /// <summary>Stance the "authored" section names. No section or no "stance" = the stance is
        /// still rolled from "stances"; a typo is refused like any other enum of the file, so a
        /// hand-placed villager can never quietly come out of the dice.</summary>
        private static Stance? ParseAuthoredStance(NpcAuthoredData? data)
        {
            string? stance = data?.Stance;
            return string.IsNullOrEmpty(stance) ? null : EnumParser.ParseEnum<Stance>(stance);
        }

        /// <summary>Rarity the "authored" section names — it wins over the older top-level "rarity"
        /// field, which keeps working alone for every record that has no section.</summary>
        private static Rarity? ParseAuthoredRarity(NpcAuthoredData? data)
        {
            string? rarity = data?.Rarity;
            return string.IsNullOrEmpty(rarity) ? null : EnumParser.ParseEnum<Rarity>(rarity);
        }

        /// <summary>Fixed rarity from the data ("rarity" field, bosses/uniques); absent = roll by weight.</summary>
        private static Rarity? ParseFixedRarity(NpcData data) =>
            string.IsNullOrEmpty(data.Rarity) ? null : EnumParser.ParseEnum<Rarity>(data.Rarity);

        private Rarity RollRarity()
        {
            long index = _rnd.RandWeighted(s_rarityWeights.Select(entry => entry.Weight).ToArray());
            return s_rarityWeights[Math.Max(0, (int)index)].Rarity;
        }

        /// <summary>The modifier count the "authored" section names, or null when it names none and
        /// the type × rarity formula answers instead. A negative number is floored to zero and not
        /// handed back as null: the record spoke, so the formula stays out, and the only reading of
        /// "fewer than no modifiers" that a spawn can act on is none.</summary>
        private static int? AuthoredModifierCount(NpcAuthoredData? data) =>
            data?.ModifierCount is int count ? Math.Max(0, count) : null;

        /// <summary>Weighted pick without replacement from the shared modifier pool. The count the
        /// "authored" section names is an ABSOLUTE — a hand-placed villager gets exactly what the file
        /// says and no dice touch it; an authored 0 leaves the loop unentered, so the npc comes out bare
        /// and no weighted pick happens at all. With no authored count, type × rarity gives the number
        /// of SLOTS and each slot is then offered to the dice at a chance that falls off slot by slot
        /// (<see cref="INpcSpawnRollsProvider"/>): the formula became a ceiling instead of a promise, so
        /// two elites of one record no longer come out identically loaded. WHICH modifiers a spawn wears
        /// stays rolled either way; overrides carry none, so nothing above this line has an opinion
        /// about the count.</summary>
        private List<INpcModifier> RollModifiers(EntityType entityType, Rarity rarity, NpcAuthoredData? authored)
        {
            int? authoredCount = AuthoredModifierCount(authored);
            int slots = authoredCount ?? NpcTypeDefaults.ModifierCount(entityType, rarity);
            var pool = modifierProvider.GetAllModifiers().ToList();

            List<INpcModifier> rolled = [];
            while (rolled.Count < slots && pool.Count > 0)
            {
                if (authoredCount == null && !FillsSlot(spawnRolls.ModifierSlotChance(entityType, rarity, rolled.Count))) break;

                long index = _rnd.RandWeighted(pool.Select(modifier => modifier.Weight).ToArray());
                var picked = pool[Math.Max(0, (int)index)];
                pool.Remove(picked);
                rolled.Add(modifierProvider.GetModifier(picked.Id));
            }

            return rolled;
        }

        /// <summary>Whether one slot is taken. A certainty (and an impossibility) costs no dice, which is
        /// what let this catalog arrive without moving a single seeded result for the types that have no
        /// ladder — but read the consequence the right way round: how many dice a spawn spends is part of
        /// the balance file now. Moving a chance ACROSS 1.0 or 0.0 in either direction adds or removes a
        /// draw, and every later draw of that spawn shifts one step along the stream with it — not just
        /// how many modifiers and abilities come out, but WHICH ones. Seeded runs and saved worlds
        /// rebuilt from a seed will not reproduce across such an edit; changing a chance strictly inside
        /// the open interval never has that effect.</summary>
        private bool FillsSlot(float chance) => chance >= 1f || (chance > 0f && _rnd.RandFloat() < chance);

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
        /// weighted pick without replacement from the archetype pool. The count is the number of SLOTS
        /// and each is offered to the dice on the ability ladder of the type, exactly as modifiers are
        /// (see <see cref="RollModifiers"/>): on a Regular a cast is an event, on an Elit one or two is
        /// the norm. Two counts stay absolute and roll no dice — the one a record names in "abilityCount"
        /// (0 included: a training dummy is allowed to have no casts at all), and
        /// <see cref="NpcTypeDefaults.AllAbilities"/>, which is the boss promise that the whole stance pool
        /// is his and not a number to whittle at.</summary>
        private List<IAbility> PickAbilities(NpcData data, NpcBehaviorData behavior, EntityType entityType, Rarity rarity)
        {
            if (data.Abilities.Count > 0) return CreateAuthoredAbilities(data);

            // Absent, not zero, is what "nothing authored" looks like — same reading as the modifier count
            // (AuthoredModifierCount), and a negative number floors to none for the same reason it does there.
            int? authoredCount = data.AbilityCount is int named ? Math.Max(0, named) : null;
            bool absolute = authoredCount != null;
            int slots = authoredCount ?? NpcTypeDefaults.DefaultAbilityCount(entityType);
            if (slots == NpcTypeDefaults.AllAbilities) absolute = true;
            var pool = behavior.Abilities.Where(entry => abilityProvider.KnownAbilityIds.Contains(entry.Id)).ToList();

            List<IAbility> picked = [];
            while (picked.Count < slots && pool.Count > 0)
            {
                if (!absolute && !FillsSlot(spawnRolls.AbilitySlotChance(entityType, rarity, picked.Count))) break;

                long index = _rnd.RandWeighted(pool.Select(entry => entry.Weight).ToArray());
                var entry = pool[Math.Max(0, (int)index)];
                pool.Remove(entry);
                picked.Add(abilityProvider.CreateAbility(entry.Id));
            }

            return picked;
        }

        /// <summary>Unknown ids are reported and skipped — a typo must not abort the whole spawn.</summary>
        private List<IAbility> CreateAuthoredAbilities(NpcData data)
        {
            List<IAbility> abilities = [];
            foreach (string abilityId in data.Abilities)
            {
                if (!abilityProvider.KnownAbilityIds.Contains(abilityId))
                {
                    Tracker.TrackNotFound($"Authored ability '{abilityId}' of npc '{data.Id}'", this);
                    continue;
                }

                abilities.Add(abilityProvider.CreateAbility(abilityId));
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
