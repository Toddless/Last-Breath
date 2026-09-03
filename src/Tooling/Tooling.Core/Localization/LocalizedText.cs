namespace Tooling.Localization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using static Tooling.Text.Format;

    /// <summary>One piece of a record's text: the suffix its catalog words it under, the key that spells
    /// out to, and the key it is laid down after when no locale has it yet — a description belongs beside
    /// the name it describes and not at the end of the file.</summary>
    public readonly record struct LocalizedTextKey(string Suffix, string Key, string? After);

    /// <summary>What a rename came to: how many keys moved, and the name that stopped it when none did.
    /// A refusal has to be able to say which word is in the way — the author is looking at an id he has
    /// just typed and nothing on screen tells him another record already wrote under it.</summary>
    public readonly record struct LocalizedRename(int Renamed, string? Taken);

    /// <summary>
    /// The wording of the game as an authoring tool edits it: one key, every locale at once. What a key
    /// is called is not decided here — a record's id and the suffixes its catalog declares are, and this
    /// only puts the two together so that one gesture reaches both files.
    /// <para>A record taken out leaves its keys standing. Whether a key is still read is a question about
    /// every catalog and about the game's own code besides, and a tool answering it one record at a time
    /// would delete the text of an id something else still names; the orphans are swept by an audit over
    /// the whole data root, which is the only place the question can be asked whole.</para>
    /// <para>That a locale has changed is heard from the stack its edits are filed on — the tool's own,
    /// where it handed one over — and never announced again from here.</para>
    /// </summary>
    public sealed class LocalizedTexts
    {
        /// <summary>The locale the author writes in, and so the one that stands first: the boxes are read
        /// top down and the top one is the one being written.</summary>
        public const string AuthoringLocale = "ru";

        /// <summary>The locale written beside it. The game reads this one first, so it is where a missing
        /// translation is noticed.</summary>
        public const string ReferenceLocale = "en";

        private readonly PoCatalogSet _set;

        /// <summary>Takes a set of locales to edit. The set decides which locales there are and in what
        /// order they are read — a view laying its rows out by a constant of its own would disagree with
        /// the files the day a third locale is added.</summary>
        public LocalizedTexts(PoCatalogSet set)
        {
            ArgumentNullException.ThrowIfNull(set);

            _set = set;
        }

        public IReadOnlyList<string> Locales => _set.Locales;

        /// <summary>Whether any locale is waiting to be written.</summary>
        public bool IsDirty => Documents().Any(document => !document.IsClean);

        public int DirtyCount => Documents().Count(document => !document.IsClean);

        /// <summary>Reads the locales of one folder in the order the tool edits them: the authoring one
        /// first, the reference one beside it — onto <paramref name="history"/> where the tool has one of
        /// its own, so that the wording is stepped by the same key as the records.</summary>
        public static LocalizedTexts Load(string folder, EditHistory? history = null) =>
            new(PoCatalogSet.Load(folder, history, AuthoringLocale, ReferenceLocale));

        /// <summary>
        /// The keys one record's text is written under, in the order the catalog declares its suffixes.
        /// Each of them names the key before it as the place to be laid down after, so a name and its
        /// description arrive in the file as neighbours rather than as two entries a section apart; the
        /// first of them follows <paramref name="after"/>, which is what keeps a fresh record inside the
        /// section its neighbours are in instead of at the end of the file.
        /// </summary>
        public static IReadOnlyList<LocalizedTextKey> Keys(string recordId, IEnumerable<string> suffixes, string? after = null)
        {
            ArgumentNullException.ThrowIfNull(recordId);
            ArgumentNullException.ThrowIfNull(suffixes);

            List<LocalizedTextKey> keys = [];

            // A record whose id is empty is listed by its place in the file, and a place words no key.
            if (recordId.Length == 0) return keys;

            string? previous = after;

            foreach (string suffix in suffixes)
            {
                string key = recordId + suffix;

                keys.Add(new LocalizedTextKey(suffix, key, previous));
                previous = key;
            }

            return keys;
        }

        /// <summary>Where a record's own keys are laid down: the last key of the record before it that
        /// some locale actually wrote. Null when that record wrote none, or when there is no record before
        /// this one — the new keys then go to the end of the files, together, which is at least a place
        /// the author can find them in.</summary>
        public string? AnchorOf(string? neighbourId, IEnumerable<string> suffixes)
        {
            ArgumentNullException.ThrowIfNull(suffixes);

            if (neighbourId is not { Length: > 0 } id) return null;

            // The last one it wrote and not the first: a record starting under the neighbour's name would
            // stand between that name and the description belonging to it.
            return Keys(id, suffixes).Select(key => key.Key).LastOrDefault(Taken);
        }

        /// <summary>What one locale says under a key, or null when that locale has no such key at all —
        /// told apart from the empty string, which is a key that is there and says nothing.</summary>
        public string? Read(string locale, string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            // The keyless entry carries the file's own settings and is not a text of the game. Nothing
            // addresses it by key, and a field whose key has not been typed yet must not reach it.
            if (key.Length == 0) return null;

            return _set.Get(locale).TryGet(key)?.MsgStr;
        }

        /// <summary>
        /// Writes what one locale says under a key, laying the key down in every locale the moment the
        /// first letter of it is written. <paramref name="after"/> is where it is laid down when no file
        /// has it yet.
        /// <para>False when nothing was written: a key nobody has and nothing to say under it is not a key
        /// worth laying down, and a plural entry is not this document's to edit one form at a time.</para>
        /// </summary>
        public bool Write(string locale, string key, string text, string? after = null)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(text);

            if (key.Length == 0) return false;

            PoDocument document = _set.Get(locale);

            // An empty box is the state every key starts in. Written down, a record merely looked at would
            // put its whole family of keys into both files.
            if (document.TryGet(key) is null && text.Length == 0) return false;

            Ensure(key, after);

            return document.Set(key, text);
        }

        /// <summary>Writes one text of a record, laying its key down after the nearest key of the record
        /// that some locale already holds. A description written before its own name would otherwise be
        /// laid down at the end of the files and the name after it, leaving the pair back to front.</summary>
        public bool Write(string locale, IReadOnlyList<LocalizedTextKey> family, int index, string text)
        {
            ArgumentNullException.ThrowIfNull(family);

            return Write(locale, family[index].Key, text, Anchor(family, index));
        }

        /// <summary>
        /// Names a record's keys again after its id was retyped, and answers with how many moved. Only the
        /// keys the catalog words from that id are touched: a key that merely begins with the old word
        /// belongs to something else, and taking it along would carry another record's text away.
        /// <para>All or none. Every name is asked for before the first key moves — a set half renamed
        /// leaves a record whose name is read under one id and whose description is read under another,
        /// and the author, looking at one box that answered and one that did not, has no gesture that puts
        /// it back.</para>
        /// <para>What step these moves are filed as is the caller's: the id in the record's own file was
        /// retyped first and belongs to the same gesture, and only the caller holds both.</para>
        /// </summary>
        public LocalizedRename RenameRecord(string oldId, string newId, IEnumerable<string> suffixes)
        {
            ArgumentNullException.ThrowIfNull(oldId);
            ArgumentNullException.ThrowIfNull(newId);

            if (oldId.Length == 0 || newId.Length == 0) return default;
            if (string.Equals(oldId, newId, StringComparison.Ordinal)) return default;

            List<LocalizedTextKey> keys = [.. Keys(oldId, suffixes)];

            if (keys.Select(key => newId + key.Suffix).FirstOrDefault(Taken) is { } taken)
                return new LocalizedRename(0, taken);

            return new LocalizedRename(keys.Count(key => _set.RenameKey(key.Key, newId + key.Suffix)), null);
        }

        /// <summary>Ends the run of keystrokes the files are taking, so the next box edited is a step of
        /// its own. Asked of every locale: one gesture is a run in one file, and which file it was is not
        /// worth remembering.</summary>
        public void Seal()
        {
            foreach (PoDocument document in Documents()) document.History.Seal();
        }

        /// <summary>Writes back every locale that has changed. A file that refused to be written is a note
        /// and not an exception, for the reason the catalogs' own save gives: the rest of the work still
        /// has to reach disk.</summary>
        /// <remarks>The result is the catalogs' own shape. "What a save wrote and what it could not" is one
        /// question, and the host puts both answers on one list.</remarks>
        public CatalogSaveResult SaveAll()
        {
            List<string> saved = [];
            List<string> notes = [];

            foreach (string locale in Locales)
            {
                if (_set.Get(locale).IsClean) continue;

                string path = _set.PathOf(locale);
                string writing = locale;

                if (CatalogSaveResult.Guarded(() => _set.Save(writing)) is { } refusal)
                    notes.Add(Text(CatalogSaveResult.UnwritableFormat, path, refusal));
                else saved.Add(path);
            }

            return new CatalogSaveResult(saved, notes);
        }

        /// <summary>The key the one at <paramref name="index"/> is laid down after: the nearest key before
        /// it in the record's own family that some locale already holds, and failing all of them the
        /// neighbour the family was built on.</summary>
        private string? Anchor(IReadOnlyList<LocalizedTextKey> family, int index)
        {
            for (int at = index - 1; at >= 0; at--)
                if (Taken(family[at].Key))
                    return family[at].Key;

            return family[0].After;
        }

        /// <summary>Whether any locale already writes something under the key. Asked the way
        /// <see cref="PoCatalogSet.RenameKey"/> asks it, so a name this calls free is a name a rename goes
        /// through on.</summary>
        private bool Taken(string key) => Documents().Any(document => document.TryGet(key) is not null);

        /// <summary>Puts the key into every locale that lacks it. Seeded beside <paramref name="after"/>
        /// first where no locale has it at all: the set places a new key next to the neighbour its files
        /// agree on, and a key nobody holds has no neighbour to agree on — it would land at the end of both
        /// files, away from the record it belongs to.</summary>
        private void Ensure(string key, string? after)
        {
            List<PoDocument> documents = [.. Documents()];

            if (documents.All(document => document.TryGet(key) is not null)) return;

            if (after is { Length: > 0 } && documents.All(document => document.TryGet(key) is null))
                Seed(documents, key, after);

            _set.EnsureKey(key);
        }

        /// <summary>Writes the key into the first locale holding the neighbour, so the set has one place to
        /// read the key's own neighbour from and gives the rest of the locales the same one.</summary>
        private static void Seed(List<PoDocument> documents, string key, string after)
        {
            foreach (PoDocument document in documents)
            {
                if (document.TryGet(after) is null) continue;
                if (document.Add(key, string.Empty, after)) return;
            }
        }

        private IEnumerable<PoDocument> Documents() => _set.Locales.Select(_set.Get);
    }
}
