namespace Battle.Source.CombatRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Data;
    using Core.Data.CombatRulesData;
    using Core.Data.GameData;
    using Core.Enums;
    using Newtonsoft.Json;

    /// <summary>Loads SharedData/CombatRules; enums are parsed at load time so a typo is a report, not a mid-fight throw.</summary>
    public class CombatRulesProvider : ICombatRulesProvider, IGameDataParticipant
    {
        public ControlResistanceRules ControlResistance { get; private set; } = ControlResistanceRules.Disabled;

        public ArenaRules Arena { get; private set; } = ArenaRules.Default;

        public ExhaustionRules Exhaustion { get; private set; } = ExhaustionRules.Disabled;

        public EffectRules Effects { get; private set; } = EffectRules.Default;

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
        }
    }
}
