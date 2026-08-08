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

        /// <summary>The augment slot that arrives together with the ability instead of through a socket
        /// node of its own. Carrying it is what makes a class an ability's way into the book, so the two
        /// are one entry in the table and cannot be answered differently.</summary>
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

        /// <summary>
        /// The neutral centre of the wheel: a start belonging to no stance, sitting where the stance
        /// seeds meet so every ray hangs off one point. It is a start because that is what the layout
        /// needs — free with the character and a root the rest of the tree grows from — and not a
        /// fourth seed: it belongs to nobody, so it hands out no ability and is not one of the
        /// <see cref="PassiveTreeDocument.StartPointCount"/> stance seeds the design counts.
        /// <para>Whoever asks whether a node is the hub asks it here. The class alone cannot answer —
        /// the hub and a seed are the same class in the file — so a second reading of "no stance" is a
        /// second answer.</para>
        /// </summary>
        public static bool IsWheelHub(PassiveNode node) => node.Kind == PassiveNodeKind.Start && node.Stance is null;

        /// <summary>A start that opens a stance: the seed one ray of the wheel begins at. Exactly the
        /// starts that are not the hub, so the two readings can never overlap or leave a gap.</summary>
        public static bool IsStanceSeed(PassiveNode node) => node.Kind == PassiveNodeKind.Start && !IsWheelHub(node);

        /// <summary>
        /// Which tier of augment slot taking the node opens, or <see cref="NoSocket"/> for the classes
        /// that open none. A total answer from the table rather than a switch with an unreachable arm:
        /// the tier is a number the socket carries, so a class opening a tier the game has not used
        /// yet is a table entry and nothing else.
        /// </summary>
        public static int SocketTier(PassiveNodeKind kind) => For(kind).SocketTier;

        /// <summary>
        /// Whether taking the node puts the ability it names into the character's book. A stance is
        /// granted its first ability at its seed and buys the rest at unlock nodes, so both classes
        /// answer yes — and both bring the tier-1 slot with them, because that slot is the ability's
        /// own and never a node. Whoever reads this asks nothing about the class beyond it: the game
        /// filling the book, the tool judging whether a socket has an ability to sit on.
        /// </summary>
        public static bool UnlocksAbility(PassiveNodeKind kind) => SocketTier(kind) == UnlockSocketTier;
    }

    /// <param name="MinModifiers">Fewer lines than this is a content error, reported but not blocked.</param>
    /// <param name="MaxModifiers">The editor stops offering "add line" past this count.</param>
    /// <param name="RequiresAbility">The node points at an ability from the game's ability catalog.</param>
    /// <param name="UsesRuleText">The node's payload is prose the summator cannot evaluate.</param>
    /// <param name="SocketTier">The augment slot the node opens on the ability it references;
    /// <see cref="NodeKindRules.NoSocket"/> when it opens none. A class opening
    /// <see cref="NodeKindRules.UnlockSocketTier"/> is a class that hands the ability over — that slot
    /// comes bundled with the ability and is not a node of its own.</param>
    public readonly record struct NodeKindRule(
        int MinModifiers,
        int MaxModifiers,
        bool RequiresAbility,
        bool UsesRuleText,
        int SocketTier = NodeKindRules.NoSocket);
}
