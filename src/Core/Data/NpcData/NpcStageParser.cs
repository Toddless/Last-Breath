namespace Core.Data.NpcData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;

    /// <summary>
    /// Strict parse of the "stages" section: enums via <see cref="EnumParser"/>, sane numbers.
    /// Unlike reactions the section is all-or-nothing: skipping one broken stage would silently
    /// shift transition indices, so any broken entry drops the WHOLE section with a report —
    /// the boss falls back to a stage-less fight instead of a half-broken one.
    /// </summary>
    public static class NpcStageParser
    {
        public static List<NpcStageConfig> Parse(string npcId, IReadOnlyList<NpcStageData> entries)
        {
            try
            {
                return entries.Select(ParseStage).ToList();
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Broken stages of npc '{npcId}' — the whole section is dropped", e);
                return [];
            }
        }

        private static NpcStageConfig ParseStage(NpcStageData entry)
        {
            if (entry.ParameterMultiplier <= 0f) throw new FormatException($"parameterMultiplier {entry.ParameterMultiplier} must be positive");
            if (entry.NextStageAtHealthPercent is <= 0f or >= 1f) throw new FormatException($"nextStageAtHealthPercent {entry.NextStageAtHealthPercent} is outside (0..1)");
            if (entry.RageAtHealthPercent is <= 0f or >= 1f) throw new FormatException($"rageAtHealthPercent {entry.RageAtHealthPercent} is outside (0..1)");
            if (entry.RageAtHealthPercent != null && entry.RageBonus is not > 0f) throw new FormatException("rageAtHealthPercent requires a positive rageBonus");

            return new NpcStageConfig(
                entry.ParameterMultiplier,
                entry.Abilities,
                entry.AttackEffects.Select(ParseAttackEffect).ToList(),
                entry.NextStageAtHealthPercent,
                entry.RageAtHealthPercent,
                entry.RageBonus ?? 0f);
        }

        private static NpcStageAttackEffectConfig ParseAttackEffect(NpcStageAttackEffectData entry)
        {
            if (entry.Chance is <= 0f or > 1f) throw new FormatException($"chance {entry.Chance} is outside (0..1]");
            if (entry.DamagePercent is not > 0f) throw new FormatException("attack effect requires a positive damagePercent (its magnitude)");
            if (entry.Duration <= 0) throw new FormatException($"duration {entry.Duration} must be positive");
            if (entry.MaxStacks <= 0) throw new FormatException($"maxStacks {entry.MaxStacks} must be positive");

            return new NpcStageAttackEffectConfig(
                EnumParser.ParseEnum<StageAttackEffectKind>(entry.Effect),
                entry.Chance,
                entry.DamagePercent.Value,
                entry.Duration,
                entry.MaxStacks);
        }
    }
}
