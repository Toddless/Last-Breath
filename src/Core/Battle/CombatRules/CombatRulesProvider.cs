namespace Core.Battle.CombatRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Data.CombatRulesData;
    using Data.GameData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>Loads SharedData/CombatRules; enums are parsed at load time so a typo is a report, not a mid-fight throw.</summary>
    public class CombatRulesProvider : ICombatRulesProvider, IGameDataParticipant
    {
        /// <summary>The lowest stage a multicast rolls for: stage 1 fires without a roll.</summary>
        private const int FirstRolledStage = 2;

        public ControlResistanceRules ControlResistance { get; private set; } = ControlResistanceRules.Disabled;

        public ArenaRules Arena { get; private set; } = ArenaRules.Default;

        public ExhaustionRules Exhaustion { get; private set; } = ExhaustionRules.Disabled;

        public EffectRules Effects { get; private set; } = EffectRules.Default;

        public MulticastRules Multicast { get; private set; } = MulticastRules.Default;

        public IReadOnlyList<string> Catalogs => [DataCatalog.CombatRules];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<CombatRulesData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize combat rules");

            var mask = data.ControlResistance.HardControlStatuses
                .Aggregate(StatusEffects.None, (current, name) => current | EnumParser.ParseEnum<StatusEffects>(name));
            var appliesTo = data.ControlResistance.AppliesTo
                .Select(EnumParser.ParseEnum<EntityType>)
                .ToHashSet();

            ControlResistance = new ControlResistanceRules(
                mask, data.ControlResistance.DurationMultipliers, appliesTo, data.ControlResistance.ResistanceDecayTurns);
            Arena = new ArenaRules(data.Arena.MaxBattleSlots);
            Exhaustion = new ExhaustionRules(data.Exhaustion.CostIncreasePerStack, data.Exhaustion.DecayPerTurn);
            Effects = new EffectRules(data.Effects.MaxExtendedTurns);
            Multicast = ParseMulticast(data.Multicast, file.FileName);
        }

        /// <summary>Stage rows of the multicast section: a row naming a stage that is never rolled or a
        /// share outside 0..1 is dropped, and a section left without a single usable row falls back to
        /// the working ladder rather than leaving the stance unable to roll.</summary>
        private static MulticastRules ParseMulticast(MulticastData data, string fileName)
        {
            List<MulticastStage> stages = [];
            foreach (MulticastStageData row in data.Stages)
            {
                if (row.Stage < FirstRolledStage || !IsShare(row.Chance) || !IsShare(row.Cap))
                {
                    Tracker.TrackError(
                        $"Combat rules '{fileName}': multicast stage {row.Stage} (chance {row.Chance}, cap {row.Cap}) is not a rollable stage — the row is skipped");
                    continue;
                }

                stages.Add(new MulticastStage(row.Stage, row.Chance, row.Cap));
            }

            if (stages.Count > 0) return new MulticastRules(stages);

            Tracker.TrackError($"Combat rules '{fileName}': the multicast section declares no rollable stage — the stance rolls on the defaults");
            return MulticastRules.Default;
        }

        private static bool IsShare(float value) => value is >= 0f and <= 1f;
    }
}
