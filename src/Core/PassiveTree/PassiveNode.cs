namespace Core.PassiveTree
{
    using System;
    using System.Collections.Generic;
    using Enums;

    /// <summary>A single point on the tree. Identity is <see cref="Id"/> — links, allocation and the save
    /// format all address a node by it, so a rename has to travel through the document.</summary>
    public sealed class PassiveNode
    {
        public string Id { get; set; } = string.Empty;

        public PassiveNodeKind Kind { get; set; } = PassiveNodeKind.Small;

        /// <summary>Which ray the node belongs to; null for the neutral core.</summary>
        public Stance? Stance { get; set; }

        /// <summary>Second ray of a hybrid node — transition nodes on a wedge between two rays, giving
        /// half to each instead of full to one. Null for everything on a single ray.</summary>
        public Stance? HybridStance { get; set; }

        public bool IsHybrid => Stance is not null && HybridStance is not null && Stance != HybridStance;

        public float X { get; set; }

        public float Y { get; set; }

        /// <summary>Display name (or a localization key once the game reads this file).</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Rule text for keystones, and a place to write down formula-shaped notables.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Ability this node unlocks or sockets into; empty for content nodes.</summary>
        public string AbilityId { get; set; } = string.Empty;

        /// <summary>Passive skill the node hands over when it is taken, built on the spot from
        /// <see cref="Properties"/>; null on a node that speaks in lines. A blank id normalizes to null.</summary>
        public string? PassiveId
        {
            get => field;
            set => field = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>Named numbers the passive is built with, in the shape a record's factory reads them —
        /// balance is authored here, never inside the skill.</summary>
        public Dictionary<string, float> Properties { get; } = [];

        public bool IsPassive => PassiveId is not null;

        /// <summary>The named numbers as an ordered list — the shape an editor renames, reorders and hands
        /// back whole.</summary>
        public List<KeyValuePair<string, float>> PropertyRows() => [.. Properties];

        /// <summary>Replaces every named number with the given ones, in the given order. Rewritten whole
        /// rather than patched: a dictionary hands a fresh key the slot a removed one left behind, so a
        /// field added after a removal would enumerate — and be written to file — where the removed field
        /// stood. Duplicate names collapse; the caller is what refuses to author them.</summary>
        public void SetProperties(IEnumerable<KeyValuePair<string, float>> rows)
        {
            Properties.Clear();
            foreach (KeyValuePair<string, float> row in rows) Properties[row.Key] = row.Value;
        }

        public List<ModifierLine> Modifiers { get; } = [];

        /// <summary>Pipeline knobs the node tunes — separate from <see cref="Modifiers"/> because the two
        /// reach a fighter by different roads (parameter resolution vs. context pipelines).</summary>
        public List<ContextModifierLine> ContextModifiers { get; } = [];

        /// <summary>The node says something in lines, on either channel. Lines and a passive are
        /// alternatives — a node carrying both is refused whole where it is read.</summary>
        public bool HasLines => Modifiers.Count > 0 || ContextModifiers.Count > 0;

        /// <summary>Why a node carrying this payload cannot be read, or null when it can. A node reaches
        /// the fighter one way: as a passive built from its own numbers, or as lines. Written both ways it
        /// promises two payloads for one point and no reader can say which one it owes. One wording for the
        /// reader that refuses such a node and the report that warns the author before he writes one.</summary>
        public static string? WhyChannelsCollide(string? passiveId, bool hasLines) =>
            !string.IsNullOrWhiteSpace(passiveId) && hasLines
                ? $"grants passive '{passiveId}' and carries modifier lines — a node speaks one way or the other"
                : null;

        /// <summary>Why the node's named numbers reach nobody, or null when they do. Numbers tuned for a
        /// passive the node never names are handed to no factory: the balance sits in the file doing
        /// nothing. One wording for the report that says so and for every tool that shows it.</summary>
        public static string? WhyPropertiesAreStranded(string? passiveId, int propertyCount) =>
            string.IsNullOrWhiteSpace(passiveId) && propertyCount > 0
                ? $"carries {propertyCount} propert(ies) but names no passive — nobody reads them"
                : null;

        /// <summary>Everything the node says in lines, both channels. The per-class content limits count
        /// content, not the road a line takes — and a composite is one piece of content however many
        /// records spell it.</summary>
        public int LineCount => LineGroups().Count;

        /// <summary>The node's records folded into the lines a player reads: records sharing a group stamp
        /// become one entry, an unstamped record is an entry of its own. Both channels flatten into one
        /// ordered list first — parametric records, then pipeline knobs — so a group sits where its first
        /// record does and a composite may reach the fighter by both roads at once.</summary>
        public List<NodeLineGroup> LineGroups()
        {
            List<NodeLineGroup> groups = [];
            var byId = new Dictionary<string, NodeLineGroup>(StringComparer.Ordinal);

            foreach (ModifierLine line in Modifiers) Place(groups, byId, line.GroupId).Modifiers.Add(line);
            foreach (ContextModifierLine line in ContextModifiers) Place(groups, byId, line.GroupId).ContextModifiers.Add(line);

            return groups;
        }

        /// <summary>The group a record joins: the one already opened under its stamp, or a fresh entry
        /// appended in reading order — which an unstamped record always gets.</summary>
        private static NodeLineGroup Place(List<NodeLineGroup> groups, Dictionary<string, NodeLineGroup> byId, string? groupId)
        {
            if (groupId is not null && byId.TryGetValue(groupId, out NodeLineGroup? existing)) return existing;

            var group = new NodeLineGroup { GroupId = groupId };
            groups.Add(group);
            if (groupId is not null) byId[groupId] = group;

            return group;
        }
    }

    /// <summary>One line of a node as content: the records that spell it, in reading order. A group of one
    /// is an ordinary line — the composite shape costs an unstamped record nothing.</summary>
    public sealed class NodeLineGroup
    {
        /// <summary>The stamp the records share, null for a record standing alone.</summary>
        public string? GroupId { get; init; }

        public List<ModifierLine> Modifiers { get; } = [];

        public List<ContextModifierLine> ContextModifiers { get; } = [];

        /// <summary>The gate the whole line hangs on — the leading record's, which every other record of
        /// the group has to repeat for the line to be printable at all.</summary>
        public string Condition => Modifiers.Count > 0 ? Modifiers[0].Condition : ContextModifiers[0].Condition;

        public bool IsConditional => !string.IsNullOrWhiteSpace(Condition);

        /// <summary>The line is measured off a carrier, so its worth is the carrier's rather than the
        /// allocation's.</summary>
        public bool IsScaled => Modifiers.Exists(line => line.IsScaled);

        /// <summary>Records of the group naming different gates — half a line held up and half not, which
        /// no single sentence can honestly say.</summary>
        public bool HasSplitCondition =>
            Modifiers.Exists(line => !string.Equals(line.Condition, Condition, StringComparison.Ordinal))
            || ContextModifiers.Exists(line => !string.Equals(line.Condition, Condition, StringComparison.Ordinal));

        /// <summary>How many records the line is spelled by — one means it is an ordinary line.</summary>
        public int PartCount => Modifiers.Count + ContextModifiers.Count;
    }
}
