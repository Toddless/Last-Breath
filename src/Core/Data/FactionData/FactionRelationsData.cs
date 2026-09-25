namespace Core.Data.FactionData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>FactionRelations.json: directed faction entries, the reputation scale, faction traits
    /// and the player's default standings (in points).</summary>
    public record FactionRelationsData
    {
        [JsonProperty("relations")] public List<FactionRelationEntry> Relations { get; init; } = [];
        [JsonProperty("reputation")] public ReputationScaleData Reputation { get; init; } = new();
        [JsonProperty("personalReputation")] public PersonalReputationData PersonalReputation { get; init; } = new();
        [JsonProperty("factions")] public List<FactionTraitsEntry> Factions { get; init; } = [];
        [JsonProperty("playerDefaults")] public List<PlayerReputationEntry> PlayerDefaults { get; init; } = [];
    }

    /// <summary>The per-NPC personal scale: points earned with one NPC shift the faction standing
    /// by whole relation-ladder steps in that NPC's eyes.</summary>
    public record PersonalReputationData
    {
        [JsonProperty("min")] public int Min { get; init; } = -1000;
        [JsonProperty("max")] public int Max { get; init; } = 1000;

        /// <summary>Every this many points (either direction) shifts the standing by one ladder step.</summary>
        [JsonProperty("pointsPerShift")] public int PointsPerShift { get; init; } = 300;

        [JsonProperty("maxShift")] public int MaxShift { get; init; } = 2;
    }

    public record FactionRelationEntry
    {
        [JsonProperty("from")][EnumOf(typeof(Fractions))] public string From { get; init; } = string.Empty;
        [JsonProperty("to")][EnumOf(typeof(Fractions))] public string To { get; init; } = string.Empty;
        [JsonProperty("level")][EnumOf(typeof(RelationLevel))] public string Level { get; init; } = string.Empty;
    }

    /// <summary>The numeric reputation scale: the points range and the level thresholds.</summary>
    public record ReputationScaleData
    {
        [JsonProperty("min")] public int Min { get; init; } = -6000;
        [JsonProperty("max")] public int Max { get; init; } = 6000;

        /// <summary>Points past a threshold required to actually switch the level (both directions) — keeps the level from flickering around a boundary.</summary>
        [JsonProperty("hysteresis")] public int Hysteresis { get; init; }

        [JsonProperty("levels")] public List<ReputationLevelEntry> Levels { get; init; } = [];
    }

    /// <summary>A level owns the points band from <see cref="From"/> up to where the next level begins.</summary>
    public record ReputationLevelEntry
    {
        [JsonProperty("level")][EnumOf(typeof(RelationLevel))] public string Level { get; init; } = string.Empty;
        [JsonProperty("from")] public int From { get; init; }
    }

    public record FactionTraitsEntry
    {
        [JsonProperty("fraction")][EnumOf(typeof(Fractions))] public string Fraction { get; init; } = string.Empty;

        /// <summary>False freezes the standing at its default — deeds are ignored (animals).</summary>
        [JsonProperty("hasReputation")] public bool HasReputation { get; init; } = true;

        /// <summary>True lets the faction raid the player at Hatred standing.</summary>
        [JsonProperty("canRaid")] public bool CanRaid { get; init; }
    }

    public record PlayerReputationEntry
    {
        [JsonProperty("fraction")][EnumOf(typeof(Fractions))] public string Fraction { get; init; } = string.Empty;
        [JsonProperty("points")] public int Points { get; init; }
    }
}
