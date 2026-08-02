namespace PassiveTreeEditor.Source.Validation
{
    using System;
    using System.Collections.Generic;
    using Core.PassiveTree;

    /// <summary>
    /// Every rule the tool checks the authored tree against, in one place and with no Godot anywhere
    /// near it: the panel only shows what comes out of here, so the rules can be read, argued with and
    /// later covered by tests without a running editor.
    /// <para>None of this is a gate. A tree is authored over weeks and is unfinished nearly all of that
    /// time; the tool always saves what it was given and only says what is unfinished.</para>
    /// </summary>
    public static class TreeValidator
    {
        /// <summary>The slot that arrives together with the ability rather than through a socket node
        /// of its own. A class carrying it is what puts an ability into the tree; a class carrying a
        /// higher tier is a socket that needs the ability to already be there.</summary>
        private const int UnlockSocketTier = 1;

        public static List<TreeIssue> Validate(PassiveTreeDocument document, AbilityCatalogView abilities)
        {
            List<TreeIssue> issues = [];

            CheckStartPoints(document, issues);
            CheckDuplicateIds(document, issues);
            CheckSockets(document, issues);
            CheckConnectivity(document, issues);

            foreach (PassiveNode node in document.Nodes)
            {
                CheckAbilityReference(node, abilities, issues);
                CheckLineCount(node, issues);
                CheckRuleText(node, issues);
                CheckStance(node, issues);
            }

            issues.Sort(Order);
            return issues;
        }

        /// <summary>Findings about the tree itself come first — they have no node to jump to and they
        /// change how the rest reads — then node by node, then rule by rule inside a node.</summary>
        private static int Order(TreeIssue first, TreeIssue second)
        {
            int byNode = string.CompareOrdinal(first.NodeId, second.NodeId);
            return byNode != 0 ? byNode : first.Kind.CompareTo(second.Kind);
        }

        private static void CheckStartPoints(PassiveTreeDocument document, List<TreeIssue> issues)
        {
            if (document.IsEmpty) return;

            int starts = 0;
            foreach (PassiveNode node in document.Nodes)
                if (node.Kind == PassiveNodeKind.Start)
                    starts++;

            if (starts == 0)
            {
                issues.Add(new TreeIssue(TreeIssueKind.NoStartPoint, string.Empty,
                    "the tree has no start point: nothing in it can be taken, and nothing is checked for reachability"));
                return;
            }

            if (starts != PassiveTreeDocument.StartPointCount)
                issues.Add(new TreeIssue(TreeIssueKind.StartPointCount, string.Empty,
                    $"the tree has {starts} start point(s), the design calls for {PassiveTreeDocument.StartPointCount}"));
        }

        /// <summary>
        /// Two nodes under one id. The live document refuses the second one at insert and the file
        /// reader reports the clash, so this fires only where neither of them stood between the author
        /// and the data — which is exactly why it is a rule and not an assumption.
        /// </summary>
        private static void CheckDuplicateIds(PassiveTreeDocument document, List<TreeIssue> issues)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (PassiveNode node in document.Nodes)
                if (!seen.Add(node.Id))
                    issues.Add(new TreeIssue(TreeIssueKind.DuplicateId, node.Id,
                        "a second node answers to this id — links and allocation address a node by it"));
        }

        /// <summary>
        /// A socket opens a slot on the ability it names. If no node of the tree unlocks that ability,
        /// the slot belongs to nothing the player can hold: the point is spent and buys an augment
        /// socket for a skill that is not in the book.
        /// </summary>
        private static void CheckSockets(PassiveTreeDocument document, List<TreeIssue> issues)
        {
            HashSet<string> unlocked = UnlockedAbilities(document);

            foreach (PassiveNode node in document.Nodes)
            {
                if (NodeKindRules.SocketTier(node.Kind) <= UnlockSocketTier) continue;

                string abilityId = node.AbilityId.Trim();
                if (abilityId.Length == 0 || unlocked.Contains(abilityId)) continue;

                issues.Add(new TreeIssue(TreeIssueKind.SocketWithoutUnlock, node.Id,
                    $"sockets '{abilityId}', which no unlock node of the tree grants — the slot cannot be reached"));
            }
        }

        private static HashSet<string> UnlockedAbilities(PassiveTreeDocument document)
        {
            var unlocked = new HashSet<string>(StringComparer.Ordinal);

            foreach (PassiveNode node in document.Nodes)
                if (NodeKindRules.SocketTier(node.Kind) == UnlockSocketTier && node.AbilityId.Trim().Length > 0)
                    unlocked.Add(node.AbilityId.Trim());

            return unlocked;
        }

        /// <summary>
        /// Two different failures, told apart on purpose. A node with no edges is a cluster that was
        /// never joined to the wheel; a node with edges that no start reaches is a whole island floating
        /// free, and only one of the two is fixed by drawing a single link.
        /// </summary>
        private static void CheckConnectivity(PassiveTreeDocument document, List<TreeIssue> issues)
        {
            HashSet<string> reached = ReachableFromStarts(document);

            foreach (PassiveNode node in document.Nodes)
            {
                if (document.Neighbours(node.Id).Count == 0)
                {
                    issues.Add(new TreeIssue(TreeIssueKind.Detached, node.Id, "not connected to anything"));
                    continue;
                }

                // With no start point nothing is reachable at all; saying so once beats saying it about
                // every node in the tree.
                if (reached.Count == 0 || reached.Contains(node.Id)) continue;

                issues.Add(new TreeIssue(TreeIssueKind.Unreachable, node.Id,
                    "no chain of edges leads here from any start point"));
            }
        }

        private static HashSet<string> ReachableFromStarts(PassiveTreeDocument document)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<string>();

            foreach (PassiveNode node in document.Nodes)
                if (node.Kind == PassiveNodeKind.Start && reached.Add(node.Id))
                    pending.Enqueue(node.Id);

            while (pending.Count > 0)
                foreach (string neighbour in document.Neighbours(pending.Dequeue()))
                    if (reached.Add(neighbour))
                        pending.Enqueue(neighbour);

            return reached;
        }

        /// <summary>
        /// Which classes name an ability is the per-class table's business, so a class that starts or
        /// stops doing so needs no edit here. An empty field is a node that does nothing; a name the
        /// catalog does not hold is a node that will do nothing once the game reads the file, which is
        /// the more expensive of the two to find out later.
        /// </summary>
        private static void CheckAbilityReference(PassiveNode node, AbilityCatalogView abilities, List<TreeIssue> issues)
        {
            string abilityId = node.AbilityId.Trim();

            if (!NodeKindRules.For(node.Kind).RequiresAbility)
            {
                if (abilityId.Length > 0)
                    issues.Add(new TreeIssue(TreeIssueKind.AbilityStray, node.Id,
                        $"a {node.Kind} node references no ability, yet still carries '{abilityId}'"));

                return;
            }

            if (abilityId.Length == 0)
            {
                issues.Add(new TreeIssue(TreeIssueKind.AbilityMissing, node.Id,
                    $"{node.Kind} must reference an ability"));
                return;
            }

            // No catalog was read at all: reporting every reference as unknown would say something
            // about the tool's own data and nothing about the tree.
            if (abilities.IsEmpty) return;

            if (!abilities.Knows(abilityId))
            {
                issues.Add(new TreeIssue(TreeIssueKind.AbilityUnknown, node.Id,
                    $"references '{abilityId}', which is in no ability catalog the tool read"));
                return;
            }

            if (!abilities.CanBeReferenced(abilityId))
                issues.Add(new TreeIssue(TreeIssueKind.AbilityHidden, node.Id,
                    $"references '{abilityId}', an internal-cast ability that never appears in a tree"));
        }

        /// <summary>Both line channels count against one limit: a line is content whichever road it
        /// takes to the fighter.</summary>
        private static void CheckLineCount(PassiveNode node, List<TreeIssue> issues)
        {
            NodeKindRule rule = NodeKindRules.For(node);

            if (node.LineCount < rule.MinModifiers)
                issues.Add(new TreeIssue(TreeIssueKind.TooFewLines, node.Id,
                    $"{node.Kind} needs at least {rule.MinModifiers} modifier line(s), has {node.LineCount}"));

            if (node.LineCount > rule.MaxModifiers)
                issues.Add(new TreeIssue(TreeIssueKind.TooManyLines, node.Id,
                    $"{node.Kind} allows at most {rule.MaxModifiers} modifier line(s), has {node.LineCount}"));
        }

        /// <summary>A keystone's payload is its rule text — it carries no lines the summator can add up,
        /// so a keystone without it is a point that buys nothing at all.</summary>
        private static void CheckRuleText(PassiveNode node, List<TreeIssue> issues)
        {
            if (!NodeKindRules.For(node.Kind).UsesRuleText) return;
            if (!string.IsNullOrWhiteSpace(node.Description)) return;

            issues.Add(new TreeIssue(TreeIssueKind.KeystoneWithoutRule, node.Id,
                $"{node.Kind} carries no rule text — its rule is the whole of what it gives"));
        }

        private static void CheckStance(PassiveNode node, List<TreeIssue> issues)
        {
            if (node.Kind != PassiveNodeKind.Start || node.Stance is not null) return;

            issues.Add(new TreeIssue(TreeIssueKind.StartWithoutStance, node.Id,
                "a start point must belong to a stance"));
        }
    }
}
