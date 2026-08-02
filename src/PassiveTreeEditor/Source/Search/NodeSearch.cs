namespace PassiveTreeEditor.Source.Search
{
    using System;
    using System.Collections.Generic;
    using Core.PassiveTree;

    /// <summary>Which field of the node answered the query. Shown next to the hit, because "why is this
    /// one here" is the first question a result list has to answer.</summary>
    public enum NodeSearchField
    {
        Id,
        Title,
        Ability
    }

    /// <param name="NodeId">The node to jump to.</param>
    /// <param name="Field">Where the query was found.</param>
    /// <param name="Text">The text of that field, so the row can show what matched.</param>
    public readonly record struct NodeSearchHit(string NodeId, NodeSearchField Field, string Text)
    {
        /// <summary>One line for the result row: the id, and the field that answered when it was not
        /// the id itself.</summary>
        public string Label => Field == NodeSearchField.Id ? NodeId : $"{NodeId}   —   {Text}";
    }

    /// <summary>
    /// Finding a node by what the author remembers about it: its id, its title, or the ability it
    /// unlocks. Plain matching over plain data — no Godot, no document state, nothing to set up — so the
    /// ranking can be read and checked on its own.
    /// </summary>
    public static class NodeSearch
    {
        /// <summary>Ranks, best first: the whole field, the start of it, somewhere inside it. A node is
        /// listed once, under the best answer any of its fields gave.</summary>
        private const int Exact = 0;
        private const int Prefix = 1;
        private const int Inside = 2;
        private const int NoMatch = int.MaxValue;

        public static List<NodeSearchHit> Find(IEnumerable<PassiveNode> nodes, string query)
        {
            List<NodeSearchHit> hits = [];

            string needle = query.Trim();
            if (needle.Length == 0) return hits;

            List<Candidate> ranked = [];
            foreach (PassiveNode node in nodes)
                if (Best(node, needle) is { } candidate)
                    ranked.Add(candidate);

            ranked.Sort(Order);

            foreach (Candidate candidate in ranked)
                hits.Add(new NodeSearchHit(candidate.NodeId, candidate.Field, candidate.Text));

            return hits;
        }

        /// <summary>Better rank first; between equal ranks the id beats the title and the title beats
        /// the ability, and ties fall back to the id so the list never reshuffles between two searches
        /// for the same thing.</summary>
        private static int Order(Candidate first, Candidate second)
        {
            if (first.Rank != second.Rank) return first.Rank.CompareTo(second.Rank);
            if (first.Field != second.Field) return first.Field.CompareTo(second.Field);
            return string.CompareOrdinal(first.NodeId, second.NodeId);
        }

        private static Candidate? Best(PassiveNode node, string needle)
        {
            Candidate? best = Better(null, node.Id, NodeSearchField.Id, node.Id, needle);
            best = Better(best, node.Title, NodeSearchField.Title, node.Id, needle);
            return Better(best, node.AbilityId, NodeSearchField.Ability, node.Id, needle);
        }

        private static Candidate? Better(Candidate? current, string text, NodeSearchField field, string nodeId, string needle)
        {
            int rank = Score(text, needle);
            if (rank == NoMatch) return current;
            if (current is { } held && held.Rank <= rank) return current;

            return new Candidate(nodeId, field, text, rank);
        }

        private static int Score(string text, string needle)
        {
            if (text.Length == 0) return NoMatch;
            if (string.Equals(text, needle, StringComparison.OrdinalIgnoreCase)) return Exact;
            if (text.StartsWith(needle, StringComparison.OrdinalIgnoreCase)) return Prefix;

            return text.Contains(needle, StringComparison.OrdinalIgnoreCase) ? Inside : NoMatch;
        }

        private readonly record struct Candidate(string NodeId, NodeSearchField Field, string Text, int Rank);
    }
}
