namespace Core.PassiveTree
{
    using System;
    using System.Collections.Generic;

    /// <summary>Per-class content rules, in one table instead of scattered switches. Authoring tools gate
    /// controls and report violations from this; nothing silently rewrites the author's data.</summary>
    public static class NodeKindRules
    {
        /// <summary>A node class that opens no augment slot.</summary>
        public const int NoSocket = 0;

        /// <summary>The augment slot bundled with the ability instead of a socket node of its own — what
        /// makes a class an ability's way into the book.</summary>
        public const int UnlockSocketTier = 1;

        private static readonly Dictionary<PassiveNodeKind, NodeKindRule> s_rules = new()
        {
            [PassiveNodeKind.Small] = new NodeKindRule(1, 2, false, false),
            [PassiveNodeKind.Notable] = new NodeKindRule(1, 3, false, false),
            [PassiveNodeKind.Keystone] = new NodeKindRule(0, 3, false, true),
            [PassiveNodeKind.AbilityUnlock] = new NodeKindRule(0, 0, true, false, SocketTier: UnlockSocketTier),
            [PassiveNodeKind.SocketTier2] = new NodeKindRule(0, 0, true, false, SocketTier: 2),
            [PassiveNodeKind.SocketTier3] = new NodeKindRule(0, 0, true, false, SocketTier: 3),
            [PassiveNodeKind.Start] = new NodeKindRule(0, 0, true, false, SocketTier: UnlockSocketTier)
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

        /// <summary>Seeds are granted with the character, so they never come out of the point budget.</summary>
        public static bool CostsPoint(PassiveNodeKind kind) => kind != PassiveNodeKind.Start;

        /// <summary>The neutral centre of the wheel: a Start with no stance, where every ray hangs off
        /// one point. Not a fourth seed — it hands out no ability and doesn't count toward
        /// <see cref="PassiveTreeDocument.StartPointCount"/>. Class alone can't tell hub from seed; this is the sole reader of "no stance".</summary>
        public static bool IsWheelHub(PassiveNode node) => node.Kind == PassiveNodeKind.Start && node.Stance is null;

        /// <summary>A start that opens a stance: the seed one ray of the wheel begins at. Exactly the
        /// starts that are not the hub, so the two readings can never overlap or leave a gap.</summary>
        public static bool IsStanceSeed(PassiveNode node) => node.Kind == PassiveNodeKind.Start && !IsWheelHub(node);

        /// <summary>Which tier of augment slot the node opens, or <see cref="NoSocket"/> for none — a
        /// table answer rather than a switch, so an unused tier is just another table entry.</summary>
        public static int SocketTier(PassiveNodeKind kind) => For(kind).SocketTier;

        /// <summary>Whether taking the node puts the ability it names into the character's book — true
        /// for a stance's seed and its unlock nodes, both of which also bring the tier-1 slot with them.</summary>
        public static bool UnlocksAbility(PassiveNodeKind kind) => SocketTier(kind) == UnlockSocketTier;
    }

    /// <param name="MinModifiers">Fewer lines than this is a content error, reported but not blocked.</param>
    /// <param name="MaxModifiers">The editor stops offering "add line" past this count.</param>
    /// <param name="RequiresAbility">The node points at an ability from the game's ability catalog.</param>
    /// <param name="UsesRuleText">The node's payload is prose the summator cannot evaluate.</param>
    /// <param name="SocketTier">Augment slot the node opens on its ability; <see cref="NodeKindRules.NoSocket"/>
    /// for none. <see cref="NodeKindRules.UnlockSocketTier"/> means the ability comes bundled, not a node of its own.</param>
    public readonly record struct NodeKindRule(
        int MinModifiers,
        int MaxModifiers,
        bool RequiresAbility,
        bool UsesRuleText,
        int SocketTier = NodeKindRules.NoSocket);
}
