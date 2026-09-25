namespace Tooling.Localization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Editing.History;

    /// <summary>How far one locale has got with a set of keys: the ones it says something for, out of the
    /// ones any locale in the set has.</summary>
    public readonly record struct PoCoverage(int Translated, int Total);

    /// <summary>
    /// The locales of one folder edited together. Each file keeps its own document, and whether they record
    /// onto one stack is the caller's to say — a key added to two locales is two steps unless the gesture
    /// that added it was opened as one, because the files are two files on disk and a save writes them one
    /// at a time. What must not go half done is a change of the key set: those are checked against every
    /// locale before the first of them is touched.
    /// </summary>
    public sealed class PoCatalogSet
    {
        public const string FileExtension = ".po";

        private readonly Dictionary<string, PoDocument> _byLocale = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _locales = [];

        private PoCatalogSet(string folder) => Folder = folder;

        /// <summary>The folder the files were read from and are written back to.</summary>
        public string Folder { get; }

        /// <summary>The locales in the order they were asked for. Everything that walks all of them walks
        /// them in this order, so a key added to several files lands in the same place in each.</summary>
        public IReadOnlyList<string> Locales => _locales;

        public static PoCatalogSet Load(string folder, params string[] locales) => Load(folder, null, locales);

        /// <summary>Reads the locales onto <paramref name="history"/> where one is given: a tool that edits
        /// the wording beside the records it belongs to steps all of it with one key.</summary>
        public static PoCatalogSet Load(string folder, EditHistory? history, params string[] locales)
        {
            ArgumentException.ThrowIfNullOrEmpty(folder);
            ArgumentNullException.ThrowIfNull(locales);

            if (locales.Length == 0) throw new ArgumentException("a catalog set needs at least one locale", nameof(locales));

            var set = new PoCatalogSet(folder);

            foreach (string locale in locales)
            {
                ArgumentException.ThrowIfNullOrEmpty(locale, nameof(locales));

                if (!set._byLocale.TryAdd(locale, PoDocument.Load(set.PathOf(locale), history)))
                {
                    throw new ArgumentException($"the locale '{locale}' is named twice", nameof(locales));
                }

                set._locales.Add(locale);
            }

            return set;
        }

        public PoDocument Get(string locale) =>
            _byLocale.TryGetValue(locale, out PoDocument? document)
                ? document
                : throw new KeyNotFoundException($"the set holds no locale '{locale}'");

        public string PathOf(string locale) => Path.Combine(Folder, locale + FileExtension);

        public void Save(string locale) => Get(locale).Save(PathOf(locale));

        public void SaveAll()
        {
            foreach (string locale in _locales)
            {
                Save(locale);
            }
        }

        /// <summary>How much of the keys under <paramref name="prefix"/> each locale actually says something
        /// for. The denominator is the same set of keys for every locale — the union the whole set knows —
        /// so the numbers can be read side by side and against <see cref="MissingIn"/>.</summary>
        public IReadOnlyDictionary<string, PoCoverage> Coverage(string prefix)
        {
            ArgumentNullException.ThrowIfNull(prefix);

            IReadOnlyList<string> keys = KeysUnder(prefix);

            return _locales.ToDictionary(
                locale => locale,
                locale => Reached(Get(locale), keys),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>The keys any locale has that this one has not, or has left empty — the work list of a
        /// translator, in the order the files put the keys in.</summary>
        public IReadOnlyList<string> MissingIn(string locale)
        {
            PoDocument target = Get(locale);

            return [.. KeysUnder(string.Empty).Where(key => !Says(target, key))];
        }

        /// <summary>Puts an empty entry for the key into every locale that lacks it, after the neighbour it
        /// has where it already exists, so a key keeps one place in all the files. True when every locale now
        /// has the key in that place; false when there was no neighbour they all share and it had to go to
        /// the end of the files instead, which is a thing the author wants to hear about.</summary>
        public bool EnsureKey(string msgId)
        {
            ArgumentNullException.ThrowIfNull(msgId);

            if (msgId.Length == 0) return false;

            List<PoDocument> lacking = [.. _locales.Select(Get).Where(document => document.TryGet(msgId) is null)];

            if (lacking.Count == 0) return true;

            string? neighbour = SharedNeighbour(msgId, lacking);
            bool placed = neighbour is not null;

            foreach (PoDocument document in lacking)
            {
                placed &= document.Add(msgId, string.Empty, neighbour);
            }

            return placed;
        }

        /// <summary>Renames the key wherever it stands, or nowhere. Checked against every locale first: a
        /// rename that went through in one file and was refused in the next would leave the two catalogs
        /// holding different sets of keys, which is the one state nothing downstream can repair.</summary>
        public bool RenameKey(string oldId, string newId)
        {
            ArgumentNullException.ThrowIfNull(oldId);
            ArgumentNullException.ThrowIfNull(newId);

            List<PoDocument> holders = [.. _locales.Select(Get).Where(document => document.TryGet(oldId) is not null)];

            if (holders.Count == 0) return false;
            if (_locales.Select(Get).Any(document => document.TryGet(newId) is not null)) return false;

            bool renamed = true;

            foreach (PoDocument document in holders)
            {
                renamed &= document.Rename(oldId, newId);
            }

            return renamed;
        }

        /// <summary>Takes the key out of every locale that has it, or out of none. Same reason as the rename:
        /// a key left standing in one file and gone from the other is a difference nobody can see.</summary>
        public bool RemoveKey(string msgId)
        {
            ArgumentNullException.ThrowIfNull(msgId);

            List<PoDocument> holders = [.. _locales.Select(Get).Where(document => document.TryGet(msgId) is not null)];

            if (holders.Count == 0) return false;
            if (holders.Any(document => document.TryGet(msgId)!.IsHeader)) return false;

            bool removed = true;

            foreach (PoDocument document in holders)
            {
                removed &= document.Remove(msgId);
            }

            return removed;
        }

        private static PoCoverage Reached(PoDocument document, IReadOnlyList<string> keys) =>
            new(keys.Count(key => Says(document, key)), keys.Count);

        private static bool Says(PoDocument document, string msgId) => document.TryGet(msgId) is { IsTranslated: true };

        /// <summary>Every key any locale has under the prefix, once each, in the order the files put them in.
        /// Coverage and the missing list are two readings of this one set.</summary>
        private IReadOnlyList<string> KeysUnder(string prefix)
        {
            List<string> keys = [];
            HashSet<string> seen = new(StringComparer.Ordinal);

            foreach (PoEntry entry in _locales.Select(Get).SelectMany(document => document.Entries))
            {
                if (entry.IsHeader || !entry.MsgId.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (!seen.Add(entry.MsgId)) continue;

                keys.Add(entry.MsgId);
            }

            return keys;
        }

        /// <summary>The key the new entry can follow in every locale that needs it. Null when the locales
        /// that have the key put it after different neighbours, or after one the others do not have: the key
        /// then goes at the end, where it is at least easy to find.</summary>
        private string? SharedNeighbour(string msgId, List<PoDocument> lacking)
        {
            foreach (PoDocument document in _locales.Select(Get))
            {
                if (document.EntryBefore(msgId)?.MsgId is not { } neighbour) continue;
                if (lacking.All(other => other.TryGet(neighbour) is not null)) return neighbour;
            }

            return null;
        }
    }
}
