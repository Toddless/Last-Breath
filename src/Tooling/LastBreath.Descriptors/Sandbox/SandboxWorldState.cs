namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Narrative.Quests;

    /// <summary>How the invisible rolls of a conversation come out while it is being read: the author
    /// asks for the outcome he wants to see rather than for a seed he would have to guess.</summary>
    public enum SandboxRolls : byte
    {
        Random,
        AlwaysPass,
        AlwaysFail,
    }

    /// <summary>
    /// The world a dry run stands in, as its author typed it: what has happened (facts), what is in the
    /// bag, where the quests stand, how skilled at words the player is and what the person across the
    /// table thinks of him.
    /// <para>Held apart from the services it is written into, because a run mutates those and the reset
    /// puts this back: the authored world is the question, the mutated one is the answer.</para>
    /// </summary>
    public sealed class SandboxWorldState
    {
        public const int DefaultInfluenceLevel = 1;

        /// <summary>The person across the table, for the entries addressed at one. Any id does: the
        /// personal layer of a dry run answers with the level its author chose, whoever is asked about.</summary>
        public const string DefaultNpcInstanceId = "DryRun_Npc";

        /// <summary>Fact key to the count standing under it; a flag is a count of one.</summary>
        public Dictionary<string, int> Facts { get; } = new(StringComparer.Ordinal);

        /// <summary>Item id to how many of it the bag holds.</summary>
        public Dictionary<string, int> Items { get; } = new(StringComparer.Ordinal);

        /// <summary>Quest id to where it stands; a quest not named here was never taken.</summary>
        public Dictionary<string, QuestStatus> Quests { get; } = new(StringComparer.Ordinal);

        public int InfluenceLevel { get; set; } = DefaultInfluenceLevel;

        public RelationLevel FactionStanding { get; set; } = RelationLevel.Neutral;

        /// <summary>What THIS npc thinks of the player — the effective relation the personal layer
        /// answers with, rather than the points behind it.</summary>
        public RelationLevel NpcRelation { get; set; } = RelationLevel.Neutral;

        public Fractions NpcFaction { get; set; } = Fractions.Human;

        public string NpcInstanceId { get; set; } = DefaultNpcInstanceId;

        public SandboxRolls Rolls { get; set; } = SandboxRolls.Random;
    }
}
