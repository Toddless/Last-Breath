namespace Core.PassiveTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The tree itself: nodes, undirected links and the point budget. Every structural change goes
    /// through this class so the id lookup, the adjacency map and the spatial index can never drift
    /// apart from the node list.
    /// </summary>
    public sealed class PassiveTreeDocument
    {
        private readonly List<PassiveNode> _nodes = [];
        private readonly Dictionary<string, PassiveNode> _byId = new(StringComparer.Ordinal);
        private readonly HashSet<NodeLink> _links = [];
        private readonly Dictionary<string, HashSet<string>> _adjacency = new(StringComparer.Ordinal);
        private readonly NodeSpatialIndex _index = new();

        private static readonly IReadOnlyCollection<string> s_noNeighbours = [];

        private bool _indexDirty = true;

        public IReadOnlyList<PassiveNode> Nodes => _nodes;

        /// <summary>No content at all: what the provider holds before the catalog is read and what the
        /// reader hands out for a file that failed to parse. An allocation cannot be checked against
        /// such a document — there is nothing for its ids to be missing from.</summary>
        public bool IsEmpty => _nodes.Count == 0;

        public IReadOnlyCollection<NodeLink> Links => _links;

        /// <summary>The spatial grid, rebuilt on first use after a change. Lazy on purpose: loading a
        /// file adds nodes one by one, and rebuilding per insert would make load quadratic.</summary>
        public NodeSpatialIndex Index
        {
            get
            {
                if (!_indexDirty) return _index;

                _index.Rebuild(_nodes);
                _indexDirty = false;
                return _index;
            }
        }

        /// <summary>Points the player is expected to have at the mastery cap. The design range is
        /// 60–65; 62 is the figure the cluster arithmetic in the draft is written against.</summary>
        public int Budget { get; set; } = DefaultBudget;

        public const int DefaultBudget = 62;

        /// <summary>Stamped on every modifier a tree node contributes. Taking a node back means
        /// dropping the modifiers carrying this source, so two spellings of it would mean lines that
        /// can be granted and never revoked.</summary>
        public const string ModifierSource = "PassiveTree";

        public PassiveNode? Find(string id) => _byId.GetValueOrDefault(id);

        public bool Contains(string id) => _byId.ContainsKey(id);

        public IReadOnlyCollection<string> Neighbours(string id) =>
            _adjacency.TryGetValue(id, out HashSet<string>? set) ? set : s_noNeighbours;

        /// <summary>Marks the spatial grid stale. Structural edits do it for you; call it after moving
        /// nodes, which touches geometry without touching the graph.</summary>
        public void Reindex() => _indexDirty = true;

        public bool AddNode(PassiveNode node)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || _byId.ContainsKey(node.Id)) return false;

            _nodes.Add(node);
            _byId[node.Id] = node;
            _adjacency[node.Id] = new HashSet<string>(StringComparer.Ordinal);
            Reindex();
            return true;
        }

        public bool RemoveNode(string id)
        {
            if (!_byId.TryGetValue(id, out PassiveNode? node)) return false;

            foreach (string neighbour in _adjacency[id].ToList()) Unlink(id, neighbour);

            _adjacency.Remove(id);
            _byId.Remove(id);
            _nodes.Remove(node);
            Reindex();
            return true;
        }

        /// <summary>Renames a node and carries every link over with it. Fails on a taken id rather
        /// than merging two nodes into one.</summary>
        public bool Rename(string oldId, string newId)
        {
            if (string.IsNullOrWhiteSpace(newId)) return false;
            if (string.Equals(oldId, newId, StringComparison.Ordinal)) return true;
            if (_byId.ContainsKey(newId) || !_byId.TryGetValue(oldId, out PassiveNode? node)) return false;

            List<string> neighbours = _adjacency[oldId].ToList();
            foreach (string neighbour in neighbours) Unlink(oldId, neighbour);

            _byId.Remove(oldId);
            _adjacency.Remove(oldId);
            node.Id = newId;
            _byId[newId] = node;
            _adjacency[newId] = new HashSet<string>(StringComparer.Ordinal);

            foreach (string neighbour in neighbours) Link(newId, neighbour);
            return true;
        }

        public bool Link(string first, string second)
        {
            if (string.Equals(first, second, StringComparison.Ordinal)) return false;
            if (!_byId.ContainsKey(first) || !_byId.ContainsKey(second)) return false;
            if (!_links.Add(NodeLink.Between(first, second))) return false;

            _adjacency[first].Add(second);
            _adjacency[second].Add(first);
            return true;
        }

        public bool Unlink(string first, string second)
        {
            if (!_links.Remove(NodeLink.Between(first, second))) return false;

            if (_adjacency.TryGetValue(first, out HashSet<string>? firstSet)) firstSet.Remove(second);
            if (_adjacency.TryGetValue(second, out HashSet<string>? secondSet)) secondSet.Remove(first);
            return true;
        }

        public bool AreLinked(string first, string second) => _links.Contains(NodeLink.Between(first, second));

        /// <summary>Unique id in the shape "kind_number", so hand-written and generated ids can live
        /// together and a fresh node never silently overwrites an existing one.</summary>
        public string NextId(PassiveNodeKind kind)
        {
            string prefix = kind.ToString().ToLowerInvariant();
            int suffix = 1;
            while (_byId.ContainsKey($"{prefix}_{suffix}")) suffix++;
            return $"{prefix}_{suffix}";
        }

        /// <summary>
        /// The next id in the same series as an existing one: "small_strength_1" begets
        /// "small_strength_2". Naming a cluster once is enough — the rest of it follows on its own.
        /// A template without a trailing number starts a series ("armor_hub" → "armor_hub_2"), and
        /// zero padding is kept ("node_01" → "node_02").
        /// </summary>
        public string NextIdFrom(string template)
        {
            if (string.IsNullOrWhiteSpace(template)) return NextId(PassiveNodeKind.Small);

            int cut = template.Length;
            while (cut > 0 && char.IsAsciiDigit(template[cut - 1])) cut--;

            string stem = template[..cut];
            string digits = template[cut..];

            if (digits.Length == 0 && !stem.EndsWith('_')) stem += "_";
            if (stem.Length == 0) stem = "node_";

            int number = digits.Length > 0 && int.TryParse(digits, out int parsed) ? parsed : 1;
            bool padded = digits.Length > 1 && digits[0] == '0';

            for (int next = number + 1; ; next++)
            {
                string candidate = stem + (padded ? next.ToString().PadLeft(digits.Length, '0') : next.ToString());
                if (!_byId.ContainsKey(candidate)) return candidate;
            }
        }

        /// <summary>Content problems worth showing the designer. Not a gate: authoring never refuses
        /// to save a half-finished tree, it only says what is unfinished.</summary>
        public List<string> Validate()
        {
            List<string> issues = [];

            foreach (PassiveNode node in _nodes)
            {
                NodeKindRule rule = NodeKindRules.For(node);

                if (node.Modifiers.Count < rule.MinModifiers)
                    issues.Add($"{node.Id}: {node.Kind} needs at least {rule.MinModifiers} modifier line(s), has {node.Modifiers.Count}");

                if (node.Modifiers.Count > rule.MaxModifiers)
                    issues.Add($"{node.Id}: {node.Kind} allows at most {rule.MaxModifiers} modifier line(s), has {node.Modifiers.Count}");

                if (rule.RequiresAbility && string.IsNullOrWhiteSpace(node.AbilityId))
                    issues.Add($"{node.Id}: {node.Kind} must reference an ability");

                if (node.Kind == PassiveNodeKind.Start && node.Stance is null)
                    issues.Add($"{node.Id}: a start point must belong to a stance");

                if (node.Kind != PassiveNodeKind.Start && _adjacency[node.Id].Count == 0)
                    issues.Add($"{node.Id}: not connected to anything");
            }

            int startCount = _nodes.Count(node => node.Kind == PassiveNodeKind.Start);
            if (_nodes.Count > 0 && startCount != StartPointCount)
                issues.Add($"the tree has {startCount} start point(s), the design calls for {StartPointCount}");

            return issues;
        }

        /// <summary>One seed per stance, granted with the character.</summary>
        public const int StartPointCount = 3;
    }
}
