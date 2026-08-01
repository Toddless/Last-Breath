namespace Core.PassiveTree
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Per-class content rules, in one table instead of scattered switches. Authoring tools use these
    /// to gate their controls and to report violations; nothing silently rewrites the author's data.
    /// </summary>
    public static class NodeKindRules
    {
        /// <summary>A node class that opens no augment slot.</summary>
        public const int NoSocket = 0;

        private static readonly Dictionary<PassiveNodeKind, NodeKindRule> s_rules = new()
        {
            [PassiveNodeKind.Small] = new NodeKindRule(1, 1, false, false),
            [PassiveNodeKind.Notable] = new NodeKindRule(1, 3, false, false),
            [PassiveNodeKind.Keystone] = new NodeKindRule(0, 3, false, true),
            [PassiveNodeKind.AbilityUnlock] = new NodeKindRule(0, 0, true, false, SocketTier: 1),
            [PassiveNodeKind.SocketTier2] = new NodeKindRule(0, 0, true, false, SocketTier: 2),
            [PassiveNodeKind.SocketTier3] = new NodeKindRule(0, 0, true, false, SocketTier: 3),
            [PassiveNodeKind.Start] = new NodeKindRule(0, 0, true, false)
        };

        /// <summary>Fails on the first use if a class was added to the enum but not to the table —
        /// louder and earlier than the lookup miss it would otherwise become mid-draw.</summary>
        static NodeKindRules()
        {
            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
                if (!s_rules.ContainsKey(kind))
                    throw new InvalidOperationException($"NodeKindRules has no entry for {kind}");
        }

        public static NodeKindRule For(PassiveNodeKind kind) => s_rules[kind];

        /// <summary>
        /// Rules for a concrete node. A hybrid small node may carry a second line, because a wedge
        /// transition is written either as one aggregate line ("+2 to all attributes") or as one line
        /// per stance ("+2 Strength and +2 Dexterity") — both shapes are in the draft.
        /// </summary>
        public static NodeKindRule For(PassiveNode node)
        {
            NodeKindRule rule = For(node.Kind);

            return node.Kind == PassiveNodeKind.Small && node.IsHybrid
                ? rule with { MaxModifiers = 2 }
                : rule;
        }

        /// <summary>Seeds are granted with the character, so they never come out of the point budget.</summary>
        public static bool CostsPoint(PassiveNodeKind kind) => kind != PassiveNodeKind.Start;

        /// <summary>
        /// Which tier of augment slot taking the node opens, or <see cref="NoSocket"/> for the classes
        /// that open none. A total answer from the table rather than a switch with an unreachable arm:
        /// the tier is a number the socket carries, so a class opening a tier the game has not used
        /// yet is a table entry and nothing else.
        /// </summary>
        public static int SocketTier(PassiveNodeKind kind) => For(kind).SocketTier;
    }

    /// <param name="MinModifiers">Fewer lines than this is a content error, reported but not blocked.</param>
    /// <param name="MaxModifiers">The editor stops offering "add line" past this count.</param>
    /// <param name="RequiresAbility">The node points at an ability from the game's ability catalog.</param>
    /// <param name="UsesRuleText">The node's payload is prose the summator cannot evaluate.</param>
    /// <param name="SocketTier">The augment slot the node opens on the ability it references;
    /// <see cref="NodeKindRules.NoSocket"/> when it opens none. The unlock node opens the tier-1 slot
    /// itself — that slot comes bundled with the ability and is not a node of its own.</param>
    public readonly record struct NodeKindRule(
        int MinModifiers,
        int MaxModifiers,
        bool RequiresAbility,
        bool UsesRuleText,
        int SocketTier = NodeKindRules.NoSocket);
}
