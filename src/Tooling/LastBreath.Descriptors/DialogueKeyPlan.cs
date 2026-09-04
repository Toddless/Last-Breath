namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Core.Data.DialogueData;
    using Core.Narrative.Dialogues;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Localization;

    /// <summary>One place a conversation reads text from: where the key is written, the key standing
    /// there now, the key the place itself words, and whether the place is an option — the one sort of
    /// place allowed to be read under a sentence no structure words.</summary>
    public readonly record struct DialogueTextPlace(JsonPointer At, string Written, string Expected, bool Offered);

    /// <summary>What one pass over a conversation came to: keys written where there was nothing to carry
    /// under them — a place holding no key, and a place whose key no locale ever wrote — keys moved
    /// together with the wording under them, the names that stopped a place (keys some locale already
    /// writes, which a place taking them would be read under another's words), and the names a wording
    /// was left parked under, which is a text no file reads until somebody names it again.</summary>
    public readonly record struct DialogueKeyResult(
        int Written, int Moved, IReadOnlyList<string> Taken, IReadOnlyList<string> Parked);

    /// <summary>One place whose wording travels with its key, and the name that wording waits under while
    /// the places of a node trade names.</summary>
    internal readonly record struct DialogueKeyCarry(DialogueTextPlace Place, string Parking);

    /// <summary>
    /// A pass over a conversation settled and not yet written: which places carry their wording to the key
    /// their place words, which are only given one, and the names that stopped the rest.
    /// <para>Settled apart from the writing because a pass that turns out to do nothing has to cost
    /// nothing: no step of a history, no file called changed.</para>
    /// </summary>
    public sealed class DialogueKeyPass
    {
        /// <summary>Whether anything at all is going to be written — into the record, or into a locale.</summary>
        public bool Writes => Moving.Count + Fresh.Count > 0;

        internal IReadOnlyList<DialogueKeyCarry> Moving { get; }

        internal IReadOnlyList<DialogueTextPlace> Fresh { get; }

        internal IReadOnlyList<string> Taken { get; }

        internal DialogueKeyPass(IReadOnlyList<DialogueKeyCarry> moving,
            IReadOnlyList<DialogueTextPlace> fresh, IReadOnlyList<string> taken)
        {
            Moving = moving;
            Fresh = fresh;
            Taken = taken;
        }
    }

    /// <summary>
    /// A conversation's localization keys read off its own structure, and brought back to it. The rule
    /// is <see cref="DialogueKeys"/>' and stated nowhere here: this only walks a dialogue as the file
    /// writes it and says which key belongs at which address.
    /// <para>What makes the walk enough to rename by is that the file remembers: the key standing on a
    /// line is what the .po files currently call it, and the structure around that line is what they
    /// have to call it now. Nothing has to be held between one gesture and the next.</para>
    /// </summary>
    public static class DialogueKeyPlan
    {
        /// <summary>What a wording on the move is parked under while the places of a node trade names.
        /// Written outside the word every conversation's keys begin with, so nothing the files hold and
        /// nothing this pass writes can land on one.</summary>
        private const string ParkingWord = "~";

        /// <summary>How a single key is handed to <see cref="LocalizedTexts.RenameRecord"/>: one suffix,
        /// and that suffix nothing. A line's key is not worded from a record's id, so the family it forms
        /// is the key itself.</summary>
        private static readonly string[] s_wholeKey = [string.Empty];

        /// <summary>The places whose key no longer answers to where they stand: a line added and not yet
        /// worded, a node renamed with its wording left behind. An option every conversation shares is
        /// none of them — it is worded from no place and moves with none; a line carrying such a key is
        /// one of them, because no line is ever read under a sentence belonging to nobody's node.</summary>
        public static IReadOnlyList<DialogueTextPlace> OffPattern(JsonTreeDocument document, JsonPointer at)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(at);

            if (document.Resolve(at) is not { } dialogue) return [];

            List<DialogueTextPlace> off = [];

            foreach (DialogueTextPlace place in Over(dialogue, at))
            {
                if (place.Expected.Length == 0) continue;
                if (string.Equals(place.Written, place.Expected, StringComparison.Ordinal)) continue;
                if (DialogueKeys.IsShared(place.Written, place.Offered)) continue;

                off.Add(place);
            }

            return off;
        }

        /// <summary>
        /// Settles what a pass would do without writing any of it: which place carries its wording to the
        /// key its place words, which is only given a key, and which is left standing under a name in the
        /// way — a key some locale already writes and nobody here is leaving, or a name a wording would
        /// have waited under, left behind by a pass that stopped half way.
        /// <para>Settled whole because a refusal makes refusals: a place left standing goes on reading the
        /// key it carries, so the place wanting that key is stopped in its turn, and so on back through the
        /// node. A pass that wrote as it walked would hand a fresh line the key of a neighbour that turns
        /// out to be staying, and the author would read one line under another's words with the report
        /// saying nothing of it.</para>
        /// </summary>
        public static DialogueKeyPass Plan(IReadOnlyList<DialogueTextPlace> places, LocalizedTexts? texts)
        {
            ArgumentNullException.ThrowIfNull(places);

            Dictionary<int, string> refused = Refused(places, texts);
            List<DialogueKeyCarry> moving = [];
            List<DialogueTextPlace> fresh = [];
            List<string> taken = [];

            for (int index = 0; index < places.Count; index++)
            {
                if (refused.TryGetValue(index, out string? name)) Name(taken, name);
                else if (Parking(places, index) is { } parking) moving.Add(new DialogueKeyCarry(places[index], parking));
                else fresh.Add(places[index]);
            }

            return new DialogueKeyPass(moving, fresh, taken);
        }

        /// <summary>
        /// Writes a settled pass: the key its place words at every place of it, and the wording the locales
        /// hold carried along with it. A place that had no key is only written — there is nothing to carry,
        /// and the boxes of the tool lay the key down in the .po files the moment somebody types into them.
        /// <para>The places of a node trade names as readily as they take fresh ones: two lines swapped
        /// each want the key the other is leaving, so every wording about to move is first parked under a
        /// name no conversation words and only then named again — asked place by place in file order, each
        /// of the two would find the other's key still in the way and neither would move.</para>
        /// </summary>
        public static DialogueKeyResult Apply(JsonTreeDocument document, DialogueKeyPass pass, LocalizedTexts? texts)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(pass);

            List<string> taken = [.. pass.Taken];
            List<string> parked = [];

            (int written, int moved) = Carried(document, pass.Moving, texts, taken, parked);

            return new DialogueKeyResult(written + Laid(document, pass.Fresh, texts, taken), moved, taken, parked);
        }

        /// <summary>Settles a pass over the places and writes it, which is the whole of one gesture.</summary>
        public static DialogueKeyResult Follow(
            JsonTreeDocument document, IReadOnlyList<DialogueTextPlace> places, LocalizedTexts? texts) =>
            Apply(document, Plan(places, texts), texts);

        /// <summary>Every place of one conversation that carries text, in the order the file writes them.</summary>
        private static IReadOnlyList<DialogueTextPlace> Over(JToken dialogue, JsonPointer at)
        {
            List<DialogueTextPlace> places = [];
            string npcId = Written(dialogue, DialoguesCatalogDescriptor.IdField);

            if (Held(dialogue, DialogueEntry.NodesKey) is not JArray nodes) return places;

            JsonPointer under = at.Append(DialogueEntry.NodesKey);

            for (int index = 0; index < nodes.Count; index++) Node(places, nodes[index], under.Append(index), npcId);

            return places;
        }

        /// <summary>Which places of the pass are refused the key their place words and the name that
        /// stopped each, asked over and over until the answer stops changing. Each round can only refuse
        /// more places than the one before it — a place refused takes the key it carries out of the ones
        /// the others may have — so the question is settled in as many rounds as the conversation has
        /// places.</summary>
        private static Dictionary<int, string> Refused(IReadOnlyList<DialogueTextPlace> places, LocalizedTexts? texts)
        {
            Dictionary<int, string> refused = [];

            while (Asking(places, texts, refused))
            {
                // Asked again on what the last round decided: a place stopped is a key nobody is leaving.
            }

            return refused;
        }

        /// <summary>One round of the question: whether the key a place words is free — claimed by no
        /// earlier place of this pass, read by no place already refused, and written by no locale except
        /// under a name this pass is leaving — and whether the wording standing there can be taken out of
        /// the way of the others at all, which a name left behind by a pass that stopped half way denies.
        /// Answers whether the round stopped anything the rounds before it had not.</summary>
        private static bool Asking(
            IReadOnlyList<DialogueTextPlace> places, LocalizedTexts? texts, Dictionary<int, string> refused)
        {
            HashSet<string> leaving = Leaving(places, refused);
            HashSet<string> standing = Standing(places, refused);
            HashSet<string> claimed = new(StringComparer.Ordinal);
            int before = refused.Count;

            for (int index = 0; index < places.Count; index++)
            {
                string expected = places[index].Expected;
                bool free = claimed.Add(expected) && !standing.Contains(expected) && !Occupied(texts, expected, leaving);

                if (!free) refused.TryAdd(index, expected);
                else if (Parking(places, index) is { } parking && Writes(texts, parking)) refused.TryAdd(index, parking);
            }

            return refused.Count > before;
        }

        /// <summary>The keys this pass is taking places off: what it may hand to another place without
        /// putting it under words written for something else.</summary>
        private static HashSet<string> Leaving(IReadOnlyList<DialogueTextPlace> places, Dictionary<int, string> refused)
        {
            HashSet<string> leaving = new(StringComparer.Ordinal);

            for (int index = 0; index < places.Count; index++)
                if (!refused.ContainsKey(index) && Carries(places[index]))
                    leaving.Add(places[index].Written);

            return leaving;
        }

        /// <summary>The keys places the pass has refused go on reading. Handed to nobody, whether or not a
        /// locale writes anything under them: two places of one conversation reading one key is the whole
        /// of what this pass is against.</summary>
        private static HashSet<string> Standing(IReadOnlyList<DialogueTextPlace> places, Dictionary<int, string> refused)
        {
            HashSet<string> standing = new(StringComparer.Ordinal);

            foreach (int index in refused.Keys)
                if (places[index].Written.Length > 0)
                    standing.Add(places[index].Written);

            return standing;
        }

        /// <summary>Whether the key standing at the place goes with it. A place with no key has nothing to
        /// carry, and one standing on a sentence every conversation shares carries nothing either: the
        /// words are the other places' too, and taking them would leave those reading nothing.</summary>
        private static bool Carries(DialogueTextPlace place) =>
            place.Written.Length > 0 && !DialogueKeys.IsShared(place.Written);

        /// <summary>What one place's wording waits under while the places of a node trade names: a name
        /// outside the word every conversation's keys begin with, told apart from the rest by the place's
        /// own number, and nothing at all where the place carries no wording to take out of the way.
        /// <para>Worded from where the place stands and not from the order the moves are made in, so the
        /// question of whether the name is free can be put before it is settled which places move.</para></summary>
        private static string? Parking(IReadOnlyList<DialogueTextPlace> places, int index) =>
            Carries(places[index])
                ? ParkingWord + index.ToString(CultureInfo.InvariantCulture) + ParkingWord + places[index].Written
                : null;

        /// <summary>Whether some locale already writes the key and no place of this pass is leaving it.</summary>
        private static bool Occupied(LocalizedTexts? texts, string key, HashSet<string> leaving) =>
            !leaving.Contains(key) && Writes(texts, key);

        /// <summary>Whether any locale writes something under the key. Unasked without the locales:
        /// nothing is being carried, and a key the files may hold is a question the pass has no file to
        /// put.</summary>
        private static bool Writes(LocalizedTexts? texts, string key)
        {
            if (texts is null) return false;

            foreach (string locale in texts.Locales)
                if (texts.Read(locale, key) is not null)
                    return true;

            return false;
        }

        /// <summary>Says the name once. A key that stopped two places is one name in the way, and saying
        /// it twice reads as two things for the author to go and look at.</summary>
        private static void Name(List<string> names, string name)
        {
            if (!names.Contains(name)) names.Add(name);
        }

        /// <summary>Writes the key its place words at every place carrying none, and answers with how many.
        /// Asked of the locales one last time and not on the strength of the plan alone: the wordings this
        /// pass moves have moved by now, and a key some locale still writes is one that was put back after
        /// a refusal — a fresh line pointed at it would be read under the words of whatever wrote it.</summary>
        private static int Laid(JsonTreeDocument document, IReadOnlyList<DialogueTextPlace> fresh,
            LocalizedTexts? texts, List<string> taken)
        {
            int written = 0;

            foreach (DialogueTextPlace place in fresh)
            {
                if (Writes(texts, place.Expected))
                {
                    Name(taken, place.Expected);
                    continue;
                }

                if (document.Put(place.At, new JValue(place.Expected))) written++;
            }

            return written;
        }

        /// <summary>Moves the wording of every place that has one and writes its new key, in two passes
        /// over the same list: out of the conversation's namespace, then into the keys the places word.
        /// A place whose wording no locale holds is written in the file and nothing is carried after it,
        /// which is a key given and not a wording moved.
        /// <para>A name that turns out to be taken after all is a locale written between the plan and the
        /// move, and nothing else: the plan ruled both refusals out. The place is left where it stands, or
        /// its wording is put back under the key it was parked from — and where even that is refused, the
        /// wording is left waiting and said out loud, because a key parked and left parked is a text no
        /// file reads.</para></summary>
        private static (int Written, int Moved) Carried(JsonTreeDocument document,
            IReadOnlyList<DialogueKeyCarry> moving, LocalizedTexts? texts, List<string> taken, List<string> parked)
        {
            int written = 0;
            int moved = 0;

            foreach ((DialogueTextPlace place, string? name, string? blocked) in Parked(moving, texts))
            {
                if (blocked is { } inTheWayOfParking)
                {
                    Name(taken, inTheWayOfParking);
                    continue;
                }

                if (name is not { } parking || texts is not { } locales)
                {
                    if (document.Put(place.At, new JValue(place.Expected))) written++;
                    continue;
                }

                if (locales.RenameRecord(parking, place.Expected, s_wholeKey).Taken is { } inTheWay)
                {
                    Name(taken, inTheWay);
                    Back(locales, parking, place, parked);
                    continue;
                }

                if (document.Put(place.At, new JValue(place.Expected))) moved++;
            }

            return (written, moved);
        }

        /// <summary>Puts a wording back under the key it was parked from, and says so when it cannot: the
        /// name it stood under has been taken by another place of this pass, and the words are left where
        /// nothing at all reads them.</summary>
        private static void Back(LocalizedTexts texts, string parking, DialogueTextPlace place, List<string> parked)
        {
            if (texts.RenameRecord(parking, place.Written, s_wholeKey).Renamed > 0) return;

            Name(parked, parking);
        }

        /// <summary>Takes every wording about to move out of the way of every other, before any of them
        /// is named again.</summary>
        private static List<DialogueKeyMove> Parked(IReadOnlyList<DialogueKeyCarry> moving, LocalizedTexts? texts)
        {
            List<DialogueKeyMove> moves = [];

            foreach (DialogueKeyCarry carry in moving)
            {
                LocalizedRename parked = texts?.RenameRecord(carry.Place.Written, carry.Parking, s_wholeKey) ?? default;

                moves.Add(new DialogueKeyMove(carry.Place, parked.Renamed > 0 ? carry.Parking : null, parked.Taken));
            }

            return moves;
        }

        private static void Node(List<DialogueTextPlace> places, JToken node, JsonPointer at, string npcId)
        {
            string nodeId = Written(node, DialogueNodeEntry.IdKey);

            if (Held(node, DialogueNodeEntry.LinesKey) is JArray lines)
            {
                JsonPointer under = at.Append(DialogueNodeEntry.LinesKey);

                for (int index = 0; index < lines.Count; index++)
                    places.Add(Place(lines[index], under.Append(index),
                        DialogueKeys.Expected(npcId, nodeId, index), offered: false));
            }

            if (Held(node, DialogueNodeEntry.OptionsKey) is not JArray options) return;

            JsonPointer offered = at.Append(DialogueNodeEntry.OptionsKey);

            for (int index = 0; index < options.Count; index++)
            {
                JToken option = options[index];
                string expected = DialogueKeys.Expected(npcId, nodeId, Written(option, DialogueNodeEntry.IdKey));

                places.Add(Place(option, offered.Append(index), expected, offered: true));
            }
        }

        private static DialogueTextPlace Place(JToken holder, JsonPointer at, string expected, bool offered) =>
            new(at.Append(DialogueLineEntry.TextKey), Written(holder, DialogueLineEntry.TextKey), expected, offered);

        /// <summary>The text a key holds, spelled the way the file spells it; empty for a key the record
        /// does not hold, one written as nothing at all, and one written as something other than a value.</summary>
        private static string Written(JToken token, string name) =>
            Held(token, name) is JValue { Value: not null } value ? JsonScalars.Written(value) : string.Empty;

        private static JToken? Held(JToken token, string name) =>
            token is JObject holder && holder.TryGetValue(name, StringComparison.Ordinal, out JToken? value)
                ? value
                : null;

        /// <summary>One place as the files answered the plan for it: <see cref="Parking"/> is where its
        /// wording is waiting now — null where no locale wrote the old key and the move is a word in the
        /// file and nothing else — and <see cref="Blocked"/> the name that stopped the wording from being
        /// taken out of the way at all.</summary>
        private readonly record struct DialogueKeyMove(DialogueTextPlace Place, string? Parking, string? Blocked);
    }
}
