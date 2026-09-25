namespace Tooling.Narrative
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using static Tooling.Text.Format;

    /// <summary>Which of the two routes out of one option an edge of the map is.</summary>
    public enum DialogueEdgeKind
    {
        /// <summary>Where the option leads when it is taken.</summary>
        Next,

        /// <summary>Where an option that rolls leads when the roll is lost.</summary>
        FailNext
    }

    /// <summary>One node of a conversation as the map draws it: the name every route calls it by, where it
    /// is written, how much it says and offers, and where a walk from the opening rules puts it.</summary>
    public sealed record DialogueGraphNode
    {
        /// <summary>The id the node is written under, or its place among its neighbours while it carries
        /// none — the way the outline names an element nobody has named yet.</summary>
        public required string Id { get; init; }

        public required JsonPointer Pointer { get; init; }

        public required int Lines { get; init; }

        public required int Options { get; init; }

        /// <summary>The column the node stands in: how many choices from an opening it is, and the band
        /// after the last of them for a node no walk arrives at.</summary>
        public required int Layer { get; init; }

        /// <summary>An entry rule opens the conversation on this node.</summary>
        public required bool Entry { get; init; }

        /// <summary>A walk from the opening rules arrives here. False is the band drawn last: the node is
        /// written and nothing in the game ever opens it.</summary>
        public required bool Reached { get; init; }

        /// <summary>The conversation stops here — the node offers no way out of itself. Said of the
        /// options it writes and not of the routes they carry: an option ending the conversation is a
        /// farewell the player presses, and a node with none leaves him with nothing to press at all.</summary>
        public bool Leaf => Options == 0;
    }

    /// <summary>One route of a conversation: the option it is written on, where it leaves and where it
    /// arrives, and everything about it a reader has to be able to see at a glance.</summary>
    public sealed record DialogueGraphEdge
    {
        /// <summary>The node the option belongs to, named as the map names it.</summary>
        public required string From { get; init; }

        /// <summary>The node the route names — whether or not the dialogue writes one.</summary>
        public required string To { get; init; }

        /// <summary>The option the route is written on: its id, or its place while it has none.</summary>
        public required string Option { get; init; }

        /// <summary>Where the option is written, so a reader may be taken to it.</summary>
        public required JsonPointer Pointer { get; init; }

        public required DialogueEdgeKind Kind { get; init; }

        /// <summary>The option is written with conditions, so the route is not always offered.</summary>
        public required bool Guarded { get; init; }

        /// <summary>No node of this dialogue answers to <see cref="To"/>: the route leads out of the
        /// conversation, which is what the loader drops the whole dialogue over.</summary>
        public required bool Dangling { get; init; }

        /// <summary>The route does not advance the conversation — it arrives in the layer it leaves or in
        /// an earlier one, which is every ring and every way back to the greeting. True inside the band of
        /// nodes nothing reaches as well: the band is one column, so nothing in it advances either.</summary>
        public bool Back { get; init; }
    }

    /// <summary>
    /// A conversation read as a map: its nodes, the routes between them, and the columns a walk from the
    /// opening rules lays them out in. Godot-free and read straight from the document, so the map answers
    /// for the file as it is being typed and not for what a loader would have kept of it.
    /// <para>A reading and not a gate: a route naming a node nobody wrote is an edge that says so, a node
    /// nothing reaches stands in a band of its own, and neither is refused. What is wrong with a
    /// conversation is the checks' answer to give; this one only shows the shape of it.</para>
    /// </summary>
    public sealed record DialogueGraph
    {
        /// <summary>What the layer of a node is while no walk has arrived at it.</summary>
        private const int Unwalked = -1;

        /// <summary>Every node the dialogue writes, in the order the file writes them.</summary>
        public required IReadOnlyList<DialogueGraphNode> Nodes { get; init; }

        /// <summary>Every route the dialogue writes, in the order the options carrying them are written.</summary>
        public required IReadOnlyList<DialogueGraphEdge> Edges { get; init; }

        /// <summary>The nodes column by column: the openings, then everything one choice away from them,
        /// and last — where there is one — the band of nodes no walk arrives at. The order inside a
        /// column is the order of the file, so a map redrawn after an edit does not reshuffle itself
        /// under the reader.</summary>
        public required IReadOnlyList<IReadOnlyList<DialogueGraphNode>> Layers { get; init; }

        /// <summary>Reads one dialogue record — the token standing at <paramref name="at"/> — as a map.</summary>
        public static DialogueGraph Read(JToken dialogue, JsonPointer at)
        {
            ArgumentNullException.ThrowIfNull(dialogue);
            ArgumentNullException.ThrowIfNull(at);

            IReadOnlyList<Place> places = Places(dialogue, at);
            IReadOnlyDictionary<string, int> addressed = Addressed(places);
            IReadOnlyList<Link> links = Links(places, addressed);
            IReadOnlyList<int> openings = Openings(dialogue, addressed);

            int[] layers = Layered(places.Count, links, openings);
            bool[] reached = [.. layers.Select(layer => layer != Unwalked)];

            Band(layers);

            IReadOnlyList<DialogueGraphNode> nodes = [.. places.Select((place, index) => new DialogueGraphNode
            {
                Id = place.Name,
                Pointer = place.Pointer,
                Lines = place.Lines,
                Options = place.Options,
                Layer = layers[index],
                Entry = openings.Contains(index),
                Reached = reached[index]
            })];

            return new DialogueGraph
            {
                Nodes = nodes,
                Edges = [.. links.Select(link => Walked(link, layers))],
                Layers = Columns(nodes)
            };
        }

        /// <summary>Every node as the file writes it, whether or not it is named, reachable or whole.</summary>
        private static List<Place> Places(JToken dialogue, JsonPointer at)
        {
            JArray nodes = NarrativeDocument.Elements(dialogue, NarrativeDocument.Nodes);
            JsonPointer list = at.Append(NarrativeDocument.Nodes);
            List<Place> places = [];

            for (int index = 0; index < nodes.Count; index++)
            {
                JToken node = nodes[index];
                string? id = Named(node);

                places.Add(new Place(
                    id ?? Text(NarrativeDocument.IndexFormat, index),
                    id,
                    node,
                    list.Append(index),
                    NarrativeDocument.Elements(node, NarrativeDocument.Lines).Count,
                    NarrativeDocument.Elements(node, NarrativeDocument.Options).Count));
            }

            return places;
        }

        /// <summary>The place a route naming an id arrives at. The FIRST node written under it: the loader
        /// refuses a dialogue that writes two and the checks name them, and a map routing to both would
        /// answer one conversation with two.</summary>
        private static Dictionary<string, int> Addressed(IReadOnlyList<Place> places)
        {
            Dictionary<string, int> addressed = new(StringComparer.Ordinal);

            for (int index = 0; index < places.Count; index++)
                if (places[index].Id is { } id)
                    addressed.TryAdd(id, index);

            return addressed;
        }

        /// <summary>Every route of every option, both of the two an option that rolls carries.</summary>
        private static List<Link> Links(IReadOnlyList<Place> places, IReadOnlyDictionary<string, int> addressed)
        {
            List<Link> links = [];

            for (int index = 0; index < places.Count; index++)
            {
                Place place = places[index];
                JArray options = NarrativeDocument.Elements(place.Token, NarrativeDocument.Options);
                JsonPointer list = place.Pointer.Append(NarrativeDocument.Options);

                for (int option = 0; option < options.Count; option++)
                {
                    JToken written = options[option];
                    var choice = new Choice(
                        index,
                        place.Name,
                        Named(written) ?? Text(NarrativeDocument.IndexFormat, option),
                        list.Append(option),
                        Gated(written));

                    Route(links, addressed, choice, written, NarrativeDocument.Next, DialogueEdgeKind.Next);

                    if (NarrativeDocument.Held(written, NarrativeDocument.SpeechCheck) is not JObject check) continue;

                    Route(links, addressed, choice, check, NarrativeDocument.FailNext, DialogueEdgeKind.FailNext);
                }
            }

            return links;
        }

        /// <summary>One route out of one option. A key nobody wrote and a key written empty are both the
        /// end of the conversation, which is a place the map has nothing to draw a line to: the game walks
        /// no route there either, and a line into nothing would read as a route that exists.</summary>
        private static void Route(
            List<Link> links,
            IReadOnlyDictionary<string, int> addressed,
            Choice choice,
            JToken holder,
            string key,
            DialogueEdgeKind kind)
        {
            if (NarrativeDocument.Written(holder, key) is not { Length: > 0 } target) return;

            int? to = addressed.TryGetValue(target, out int place) ? place : null;

            links.Add(new Link(choice.From, to, new DialogueGraphEdge
            {
                From = choice.Owner,
                To = target,
                Option = choice.Name,
                Pointer = choice.Where,
                Kind = kind,
                Guarded = choice.Guarded,
                Dangling = to is null
            }));
        }

        /// <summary>The nodes the entry rules open the conversation on, in the order the rules are
        /// written. A rule naming a node nobody wrote opens nothing: there is no place to start a walk at,
        /// so the conversation is drawn as the unreached band it would be played as.</summary>
        private static List<int> Openings(JToken dialogue, IReadOnlyDictionary<string, int> addressed)
        {
            List<int> openings = [];

            foreach (JToken rule in NarrativeDocument.Elements(dialogue, NarrativeDocument.EntryRules))
                if (NarrativeDocument.Written(rule, NarrativeDocument.Node) is { Length: > 0 } node
                    && addressed.TryGetValue(node, out int place)
                    && !openings.Contains(place))
                    openings.Add(place);

            return openings;
        }

        /// <summary>
        /// The column of every node: the openings, then a column per choice away from them, and
        /// <see cref="Unwalked"/> for everything the walk never arrives at.
        /// </summary>
        /// <remarks>A walk of this reading's own. The game's own is written over the records a loader kept
        /// (<c>NarrativeChecks</c>, which names what it cannot reach) and this one is written over a
        /// document being typed into, where a node may have no id yet and two may share one; both start at
        /// the entry rules and follow the two routes of every option, so they name the same nodes.</remarks>
        private static int[] Layered(int places, IReadOnlyList<Link> links, IReadOnlyList<int> openings)
        {
            int[] layers = new int[places];
            Array.Fill(layers, Unwalked);

            ILookup<int, Link> leaving = links.ToLookup(link => link.From);
            Queue<int> pending = new();

            foreach (int opening in openings)
            {
                layers[opening] = 0;
                pending.Enqueue(opening);
            }

            while (pending.Count > 0)
            {
                int place = pending.Dequeue();

                foreach (Link link in leaving[place])
                {
                    if (link.To is not { } next || layers[next] != Unwalked) continue;

                    layers[next] = layers[place] + 1;
                    pending.Enqueue(next);
                }
            }

            return layers;
        }

        /// <summary>Puts everything the walk never arrived at in one band after the last column it did.
        /// A band and not a column of nowhere: the nodes are written, an author is reading them, and they
        /// carry the mark that says no route of his conversation opens them.</summary>
        private static void Band(int[] layers)
        {
            int walked = 0;

            foreach (int layer in layers) walked = Math.Max(walked, layer + 1);

            for (int index = 0; index < layers.Length; index++)
                if (layers[index] == Unwalked)
                    layers[index] = walked;
        }

        /// <summary>An edge as it is drawn: marked where it leads back into ground the walk has already
        /// covered, which is a column no deeper than the one it leaves.</summary>
        private static DialogueGraphEdge Walked(Link link, IReadOnlyList<int> layers) =>
            link.To is { } to && layers[to] <= layers[link.From] ? link.Edge with { Back = true } : link.Edge;

        private static List<IReadOnlyList<DialogueGraphNode>> Columns(IReadOnlyList<DialogueGraphNode> nodes)
        {
            List<IReadOnlyList<DialogueGraphNode>> columns = [];
            int count = 0;

            foreach (DialogueGraphNode node in nodes) count = Math.Max(count, node.Layer + 1);

            for (int column = 0; column < count; column++)
                columns.Add([.. nodes.Where(node => node.Layer == column)]);

            return columns;
        }

        /// <summary>The id an element is written under, or null while it carries none — an author writes
        /// the routes before he writes the names, and a half-written name is not one a route can find.</summary>
        private static string? Named(JToken token) =>
            NarrativeDocument.Written(token, NarrativeDocument.Id) is { Length: > 0 } id ? id : null;

        /// <summary>Whether an option is written with conditions of either kind — the one deciding whether
        /// the player is shown the choice, the one deciding whether he may take it. A clause written as
        /// something other than a list is still a gate its author wrote: the map says the route is not
        /// always offered and the checks say what is wrong with the clause.</summary>
        private static bool Gated(JToken option) =>
            Gate(option, NarrativeDocument.VisibleConditions) || Gate(option, NarrativeDocument.EnabledConditions);

        private static bool Gate(JToken option, string key) =>
            NarrativeDocument.Held(option, key) switch
            {
                JArray clauses => clauses.Count > 0,
                { Type: not JTokenType.Null } => true,
                _ => false
            };

        /// <summary>One node as the file writes it: what the map calls it, the id routes may find it by —
        /// none while it is unnamed — the token its options are read out of, its address, and its counts.</summary>
        private sealed record Place(
            string Name, string? Id, JToken Token, JsonPointer Pointer, int Lines, int Options);

        /// <summary>One option, held while both of its routes are read off it.</summary>
        private sealed record Choice(int From, string Owner, string Name, JsonPointer Where, bool Guarded);

        /// <summary>One route with the places it joins, which is what the walk follows; the edge itself
        /// names them the way a reader reads them.</summary>
        private sealed record Link(int From, int? To, DialogueGraphEdge Edge);
    }
}
