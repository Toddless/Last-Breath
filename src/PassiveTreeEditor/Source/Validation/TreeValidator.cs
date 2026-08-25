namespace PassiveTreeEditor.Source.Validation
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
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
                CheckPassive(node, issues);
                CheckLineCount(node, issues);
                CheckRuleText(node, issues);
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

        /// <summary>
        /// The seeds a character is granted. The design has one at the core of the wheel, belonging to
        /// no stance and opening nothing, and one on each stance, and a stance seed is where that
        /// stance's first ability comes from — so the roster is checked place by place rather than by
        /// counting: three seeds on one stance and none on another add up to the right total and are
        /// not a tree anyone can play.
        /// </summary>
        private static void CheckStartPoints(PassiveTreeDocument document, List<TreeIssue> issues)
        {
            if (document.IsEmpty) return;

            List<PassiveNode> starts = [];
            foreach (PassiveNode node in document.Nodes)
                if (node.Kind == PassiveNodeKind.Start)
                    starts.Add(node);

            if (starts.Count == 0)
            {
                issues.Add(new TreeIssue(TreeIssueKind.NoStartPoint, string.Empty,
                    "the tree has no start point: nothing in it can be taken, and nothing is checked for reachability"));
                return;
            }

            CheckCoreStart(starts, issues);

            foreach (Stance stance in Enum.GetValues<Stance>())
                CheckStanceStart(starts, stance, issues);
        }

        /// <summary>The neutral seed in the middle of the wheel: it is the one point of the tree that
        /// belongs to no ray, and it opens nothing — an ability on it would be an ability granted
        /// outside every stance.</summary>
        private static void CheckCoreStart(List<PassiveNode> starts, List<TreeIssue> issues)
        {
            List<PassiveNode> core = starts.FindAll(node => node.Stance is null);

            if (core.Count == 0)
            {
                issues.Add(new TreeIssue(TreeIssueKind.StartPointMissing, string.Empty,
                    "no start point at the core: the design has one seed outside every stance, and the rays are reached through it"));
                return;
            }

            ReportExtras(core, "a second start point outside every stance — the core is one seed", issues);

            foreach (PassiveNode node in core) CheckCarriesNoAbility(node, issues);
        }

        /// <summary>One seed per stance, and each of them is what puts that stance's first ability in
        /// the book — a stance seed without one leaves the stance unplayable from its own start.</summary>
        private static void CheckStanceStart(List<PassiveNode> starts, Stance stance, List<TreeIssue> issues)
        {
            List<PassiveNode> seeds = starts.FindAll(node => node.Stance == stance);

            if (seeds.Count == 0)
            {
                issues.Add(new TreeIssue(TreeIssueKind.StartPointMissing, string.Empty,
                    $"no start point for {stance}: the design has one seed per stance, and it is what opens that stance's first ability"));
                return;
            }

            ReportExtras(seeds, $"a second start point for {stance} — a stance begins at one seed", issues);

            foreach (PassiveNode node in seeds) CheckCarriesAbility(node, issues);
        }

        /// <summary>Every seed past the first one of its place. The first is left alone on purpose:
        /// one of them is the seed the design asks for, and which one that is belongs to the author.
        /// </summary>
        private static void ReportExtras(List<PassiveNode> seeds, string message, List<TreeIssue> issues)
        {
            for (int index = 1; index < seeds.Count; index++)
                issues.Add(new TreeIssue(TreeIssueKind.StartPointExtra, seeds[index].Id, message));
        }

        private static void CheckCarriesAbility(PassiveNode node, List<TreeIssue> issues)
        {
            if (node.AbilityId.Trim().Length > 0) return;

            issues.Add(new TreeIssue(TreeIssueKind.AbilityMissing, node.Id,
                "a stance start point must reference the ability it opens"));
        }

        private static void CheckCarriesNoAbility(PassiveNode node, List<TreeIssue> issues)
        {
            string abilityId = node.AbilityId.Trim();
            if (abilityId.Length == 0) return;

            issues.Add(new TreeIssue(TreeIssueKind.AbilityStray, node.Id,
                $"the core start point opens no ability, yet carries '{abilityId}'"));
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
                // The bundled tier-1 slot needs no unlock elsewhere: it is the unlock.
                if (NodeKindRules.SocketTier(node.Kind) <= NodeKindRules.UnlockSocketTier) continue;

                string abilityId = node.AbilityId.Trim();
                if (abilityId.Length == 0 || unlocked.Contains(abilityId)) continue;

                issues.Add(new TreeIssue(TreeIssueKind.SocketWithoutUnlock, node.Id,
                    $"sockets '{abilityId}', which nothing in the tree opens — the slot cannot be reached"));
            }
        }

        private static HashSet<string> UnlockedAbilities(PassiveTreeDocument document)
        {
            var unlocked = new HashSet<string>(StringComparer.Ordinal);

            // Which classes put an ability in the book is the per-class table's answer, and the game
            // reads the same one — a rule kept in two places is a rule the tool and the battle can
            // disagree about.
            foreach (PassiveNode node in document.Nodes)
                if (NodeKindRules.UnlocksAbility(node.Kind) && node.AbilityId.Trim().Length > 0)
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

            // Whether a seed carries an ability depends on which seed it is — the core carries none and
            // a stance seed must — so the start rules answer that, and only the name is checked here.
            if (node.Kind == PassiveNodeKind.Start)
            {
                CheckAbilityName(node, abilityId, abilities, issues);
                return;
            }

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

            CheckAbilityName(node, abilityId, abilities, issues);
        }

        /// <summary>The name a node does carry, whether or not it had to. An empty field says nothing
        /// here — the rule that knows whether this node needed one has already spoken.</summary>
        private static void CheckAbilityName(PassiveNode node, string abilityId, AbilityCatalogView abilities, List<TreeIssue> issues)
        {
            // No catalog was read at all: reporting every reference as unknown would say something
            // about the tool's own data and nothing about the tree.
            if (abilityId.Length == 0 || abilities.IsEmpty) return;

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

        /// <summary>
        /// What the node's passive is, said where the whole tree is swept rather than only where one node
        /// is open in the inspector. Both rules are the ones the reader itself applies: a node written on
        /// both channels is dropped whole when the file is read, and numbers naming no passive are handed
        /// to no factory at all.
        /// </summary>
        private static void CheckPassive(PassiveNode node, List<TreeIssue> issues)
        {
            if (PassiveNode.WhyChannelsCollide(node.PassiveId, node.HasLines) is { } collision)
                issues.Add(new TreeIssue(TreeIssueKind.ChannelsCollide, node.Id, collision));

            if (PassiveNode.WhyPropertiesAreStranded(node.PassiveId, node.Properties.Count) is { } stranded)
                issues.Add(new TreeIssue(TreeIssueKind.PropertiesStranded, node.Id, stranded));
        }

        /// <summary>Both line channels count against one limit: a line is content whichever road it
        /// takes to the fighter. A passive is the node's payload in place of lines, so the floor is not
        /// asked of it — the ceiling still is, because a node holding both is already reported.</summary>
        private static void CheckLineCount(PassiveNode node, List<TreeIssue> issues)
        {
            NodeKindRule rule = NodeKindRules.For(node.Kind);

            if (!node.IsPassive && node.LineCount < rule.MinModifiers)
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
    }
}
