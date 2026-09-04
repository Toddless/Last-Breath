namespace Tooling.Catalogs.Checks
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Localization;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>
    /// The catalogs of a run held against each other and against the wording beside them: the ids they
    /// name, the names they are found by, the shapes their records are written in and the keys their
    /// text is read under. Everything here is answered from a schema and a document and from nothing
    /// else, so the authoring tool and the game's own tests run the very same rules.
    /// <para>A REPORT and not a gate. Nothing is refused, nothing is repaired: a finding says where an
    /// author has to look, and whether it is owed or broken is his to decide.</para>
    /// <para>The findings arrive in one order every run — catalog, then file, then the address inside it
    /// — so a run can be pinned and two runs can be compared.</para>
    /// </summary>
    public static class CatalogChecks
    {
        /// <summary>What a range is written with. The two names the game's converters know, matched
        /// without regard to case the way they match them.</summary>
        private const string MinBound = "min";

        private const string MaxBound = "max";

        /// <summary>How many ends a pair of bounds has.</summary>
        private const int Bounds = 2;

        /// <summary>How many shapes a record wearing one may wear.</summary>
        private const int OneShape = 1;

        /// <summary>An address inside a run: the file and the pointer into it.</summary>
        private const string PlaceFormat = "{0}{1}";

        /// <summary>Where a line of a locale stands.</summary>
        private const string LineFormat = "{0}:{1}";

        private const string UnknownReferenceFormat =
            "'{0}' is written where a record of {1} is meant, and none of them holds one under it.";

        private const string UndescribedTargetFormat =
            "this build has no describer for '{0}', so every id pointing there is left unanswered rather than called broken.";

        private const string DuplicateIdFormat =
            "'{0}' names {1} records of '{2}': whichever of them the reader keeps, the rest are out of reach.";

        private const string MissingTextFormat = "'{0}' is worded in no locale of the run, so the key itself reaches the player.";

        private const string UntranslatedTextFormat = "'{0}' is written in '{1}' and missing from '{2}'.";

        private const string DuplicateKeyFormat = "'{0}' is written again here; gettext keeps the first, and this one is out of the game.";

        private const string ScalarRangeFormat =
            "a range is written as {0}: one field has one shape wherever it stands, and the second spelling is one every reader has to know about.";

        private const string UnboundedRangeFormat = "a range writes no number under '{0}', which the converter reads as a bound that is not there.";

        private const string NoShapeFormat = "the record names none of {0}, so it describes nothing the reader can act on.";

        private const string TwoShapesFormat = "the record names {0} of {1} at once, which leaves what it means undecided.";

        private const string ShapeSeparator = ", ";

        /// <summary>
        /// Every rule over the catalogs of one run, in one pass.
        /// </summary>
        /// <param name="references">The ids the run knows, which is what says whether a word written where
        /// a record is meant answers to anything.</param>
        /// <param name="texts">The locales beside the catalogs, or null for a run that reads none — the
        /// wording is then not held at all rather than reported missing everywhere.</param>
        /// <param name="stepOver">Records whose ids the walk must not read as references, with everything
        /// under them. Named by the host: WHICH written word is a reference despite the markup around it
        /// is a fact about the game's own parsers, and a library over schemas cannot know it.</param>
        /// <param name="readsInOrder">Records whose shapes the game's own reader takes IN ORDER, the first
        /// key written deciding and the rest carrying whatever else the record wants to say. Named by the
        /// host for the same reason: the schema states which keys pick which shape and never whether they
        /// exclude each other. Such a record is still held to wearing one — a record naming none of its
        /// shapes describes nothing whichever way the reader goes about it.</param>
        public static IReadOnlyList<CatalogFinding> Run(
            CatalogWorkspace workspace,
            ReferenceIndex references,
            LocalizedTexts? texts = null,
            Func<JObject, bool>? stepOver = null,
            Func<RecordSchema, bool>? readsInOrder = null)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            ArgumentNullException.ThrowIfNull(references);

            List<CatalogFinding> found = [];
            Once said = new();

            foreach (CatalogView catalog in workspace.Catalogs)
                Catalog(catalog, references, texts, stepOver, readsInOrder, said, found);

            Repeated(texts, found);

            return found;
        }

        /// <summary>One catalog, file by file and record by record. The records are read out of the
        /// documents rather than out of the list the catalog was opened with: a record added, taken out or
        /// retyped since is exactly what a check pressed a second time exists to answer for.</summary>
        private static void Catalog(
            CatalogView catalog,
            ReferenceIndex references,
            LocalizedTexts? texts,
            Func<JObject, bool>? stepOver,
            Func<RecordSchema, bool>? readsInOrder,
            Once said,
            ICollection<CatalogFinding> found)
        {
            List<string> unheard = [];
            Names named = new();

            foreach (CatalogFile file in catalog.Files)
                foreach (CatalogRecord record in CatalogRecords.Read(catalog.Schema, file, unheard))
                {
                    List<ReferenceMention> mentions = [];
                    List<SchemaNode> nodes = [];

                    ReferenceWalk.Record(file, record.Schema, record.Token, record.Pointer, vocabulary: null, mentions, nodes);

                    Pointed(catalog, record, mentions, references, stepOver, said, found);
                    Shaped(catalog, record, file, record.Pointer, record.Schema, record.Token, readsInOrder, found);

                    foreach (SchemaNode node in nodes)
                    {
                        Ranged(catalog, record, node, found);
                        Shaped(catalog, record, node.File, node.At, node.Field.Record, node.Token, readsInOrder, found);
                    }

                    Worded(catalog, record, texts, said, found);
                    named.Take(catalog.Schema, record);
                }

            named.Twice(catalog.Catalog, found);
        }

        // ── the words a record writes ──────────────────────────────────────────────────────────────

        /// <summary>Every id one record writes where another record is meant, held against what the run
        /// knows. A field pointing into a catalog nobody described is passed over whole and the target is
        /// named once: half an answer would call every id of the other half broken.</summary>
        private static void Pointed(
            CatalogView catalog,
            CatalogRecord record,
            IReadOnlyList<ReferenceMention> mentions,
            ReferenceIndex references,
            Func<JObject, bool>? stepOver,
            Once said,
            ICollection<CatalogFinding> found)
        {
            foreach (ReferenceMention mention in mentions)
            {
                if (SteppedOver(mention.Where, stepOver)) continue;

                IReadOnlyList<string> missing = references.Undescribed(mention.Targets);

                if (missing.Count > 0)
                {
                    Undescribed(missing, said, found);
                    continue;
                }

                if (references.Exists(mention.Targets, mention.Id)) continue;

                found.Add(new CatalogFinding(
                    CatalogFindingKind.UnknownReference,
                    catalog.Catalog,
                    record.CurrentId,
                    mention.Id,
                    Place(mention.Where.File, mention.Where.At),
                    Text(UnknownReferenceFormat, mention.Id, Targets(mention.Targets))));
            }
        }

        /// <summary>The targets nobody described, each said once for the whole run: the same catalog is
        /// pointed at from hundreds of fields, and hundreds of rows saying one thing is a list nobody
        /// reads.</summary>
        private static void Undescribed(
            IReadOnlyList<string> missing, Once said, ICollection<CatalogFinding> found)
        {
            foreach (string target in missing)
            {
                if (!said.Target(target)) continue;

                found.Add(new CatalogFinding(
                    CatalogFindingKind.UndescribedTarget,
                    Catalog: string.Empty,
                    Record: string.Empty,
                    target,
                    Where: string.Empty,
                    Text(UndescribedTargetFormat, target)));
            }
        }

        /// <summary>Whether the word stands inside a record the host asked the walk to step over, itself
        /// included. Asked of the addresses rather than by walking again: the descent is one, and the
        /// record a word sits in is every node above it.</summary>
        private static bool SteppedOver(ReferenceUse use, Func<JObject, bool>? stepOver)
        {
            if (stepOver is null) return false;

            for (JsonPointer? at = use.At; at is not null; at = at.Parent)
                if (use.File.Document.Resolve(at) is JObject holder && stepOver(holder))
                    return true;

            return false;
        }

        // ── the shape a value is written in ────────────────────────────────────────────────────────

        /// <summary>A field the schema reads as a pair of bounds, held to that shape. WHICH fields those
        /// are is read off the schema rather than listed: a DTO that grows a range joins the rule without
        /// anyone remembering it.</summary>
        private static void Ranged(
            CatalogView catalog, CatalogRecord record, SchemaNode node, ICollection<CatalogFinding> found)
        {
            if (node.Field is not { Kind: FieldKind.Object, Record: { } shape } || !IsRange(shape)) return;

            if (node.Token is not JObject written)
            {
                found.Add(Malformed(catalog, record, node, Text(ScalarRangeFormat, node.Token.Type)));
                return;
            }

            foreach (FieldSchema bound in shape.Fields)
                if (!bound.Hidden && !IsNumber(written, bound.JsonName))
                    found.Add(Malformed(catalog, record, node, Text(UnboundedRangeFormat, bound.JsonName)));
        }

        private static CatalogFinding Malformed(
            CatalogView catalog, CatalogRecord record, SchemaNode node, string message) =>
            new(CatalogFindingKind.MalformedRange,
                catalog.Catalog,
                record.CurrentId,
                node.Field.JsonName,
                Place(node.File, node.At),
                message);

        /// <summary>Whether a record is a pair of bounds and nothing else: two numbers named min and max,
        /// which is the shape the game's converters read a range in. What a converter keeps for itself is
        /// passed over — it is no key of the file.</summary>
        private static bool IsRange(RecordSchema record)
        {
            int bounds = 0;

            foreach (FieldSchema field in record.Fields)
            {
                if (field.Hidden) continue;
                if (field.Kind is not (FieldKind.Integer or FieldKind.Number)) return false;
                if (!IsBound(field.JsonName)) return false;

                bounds++;
            }

            return bounds == Bounds;
        }

        private static bool IsBound(string jsonName) =>
            string.Equals(jsonName, MinBound, StringComparison.OrdinalIgnoreCase)
            || string.Equals(jsonName, MaxBound, StringComparison.OrdinalIgnoreCase);

        private static bool IsNumber(JObject written, string jsonName) =>
            written.GetValue(jsonName, StringComparison.OrdinalIgnoreCase)
                is { Type: JTokenType.Integer or JTokenType.Float };

        /// <summary>
        /// A record whose shapes are told apart by the PRESENCE of a key, held to wearing exactly one of
        /// them. A shape named by the VALUE of a field is another rule and belongs to whoever owns that
        /// field's words.
        /// </summary>
        /// <remarks>A key written empty is not a shape worn: the reader is looking for a word and finds
        /// none, which is why a position naming an empty id and no group is one it refuses.</remarks>
        private static void Shaped(
            CatalogView catalog,
            CatalogRecord record,
            CatalogFile file,
            JsonPointer at,
            RecordSchema? shape,
            JToken? token,
            Func<RecordSchema, bool>? readsInOrder,
            ICollection<CatalogFinding> found)
        {
            if (shape?.Variants is not { Discriminator: null } variants) return;
            if (token is not JObject holder) return;

            int worn = 0;

            foreach (VariantSchema variant in variants.Variants)
                if (Wears(holder, variant.DiscriminatorValue))
                    worn++;

            if (worn == OneShape) return;

            // Where the reader takes the shapes in order, a record wearing several is answered by the
            // first of them and everything under the rest is what else it had to say. Wearing none is
            // still nothing to read, whichever way the reader goes about it.
            if (worn > OneShape && readsInOrder?.Invoke(shape) == true) return;

            found.Add(new CatalogFinding(
                CatalogFindingKind.AmbiguousShape,
                catalog.Catalog,
                record.CurrentId,
                Named: string.Empty,
                Place(file, at),
                worn == 0
                    ? Text(NoShapeFormat, Shapes(variants))
                    : Text(TwoShapesFormat, worn, Shapes(variants))));
        }

        /// <summary>Whether a key is written as a shape the record wears. A word of no length is not one:
        /// the reader is looking for a word and finds none. Neither is a flag written false — a key whose
        /// whole content is a claim says, written that way, that the record does NOT make it.</summary>
        private static bool Wears(JObject holder, string key)
        {
            if (!holder.TryGetValue(key, StringComparison.Ordinal, out JToken? written)) return false;
            if (written.Type == JTokenType.Null) return false;
            if (written.Type == JTokenType.Boolean) return written.Value<bool>();

            return written is not JValue value || JsonScalars.Written(value).Length > 0;
        }

        private static string Shapes(VariantSet variants)
        {
            List<string> keys = [];

            foreach (VariantSchema variant in variants.Variants) keys.Add(variant.DiscriminatorValue);

            return string.Join(ShapeSeparator, keys);
        }

        // ── the wording beside the records ─────────────────────────────────────────────────────────

        /// <summary>The keys one record's catalog words its text under, held against the locales the run
        /// read. A catalog that words nothing is passed over, and so is a record listed by its place in a
        /// file: a place spells no key.</summary>
        private static void Worded(
            CatalogView catalog, CatalogRecord record, LocalizedTexts? texts, Once said, ICollection<CatalogFinding> found)
        {
            if (texts is null || catalog.Schema.LocalizedSuffixes.Count == 0) return;
            if (!CatalogRecords.Names(catalog.Schema, record.Schema)) return;
            if (record.CurrentId is not { Length: > 0 } id) return;

            foreach (LocalizedTextKey key in LocalizedTexts.Keys(id, catalog.Schema.LocalizedSuffixes))
                Read(catalog, record, key.Key, Owed(catalog.Schema, key.Suffix), texts, said, found);
        }

        /// <summary>Whether the catalog owes the key under one of its suffixes for every record, or only
        /// offers a place for it. A key that is not owed is held against the other locales all the same
        /// once some locale words it: what is written in one language and not in another is a translation
        /// still to be written, whoever asked for the line.</summary>
        private static bool Owed(CatalogSchema schema, string suffix)
        {
            foreach (string required in schema.RequiredSuffixes)
                if (string.Equals(required, suffix, StringComparison.Ordinal))
                    return true;

            return false;
        }

        /// <summary>One key across every locale: missing from the reference one is a name the player reads
        /// as an id, and missing from another is a translation still to be written.</summary>
        private static void Read(
            CatalogView catalog,
            CatalogRecord record,
            string key,
            bool owed,
            LocalizedTexts texts,
            Once said,
            ICollection<CatalogFinding> found)
        {
            if (texts.Read(LocalizedTexts.ReferenceLocale, key) is null)
            {
                // The run's one saying of the key is spent where it is OWED and nowhere else: two catalogs
                // may word their records under one key, and a catalog merely OFFERING it must not take the
                // saying and leave the one owing it silent.
                if (owed && said.Key(key))
                    found.Add(Wording(CatalogFindingKind.MissingText, catalog, record, key,
                        Text(MissingTextFormat, key)));

                return;
            }

            if (!said.Key(key)) return;

            foreach (string locale in texts.Locales)
            {
                if (string.Equals(locale, LocalizedTexts.ReferenceLocale, StringComparison.Ordinal)) continue;
                if (texts.Read(locale, key) is not null) continue;

                found.Add(Wording(CatalogFindingKind.UntranslatedText, catalog, record, key,
                    Text(UntranslatedTextFormat, key, LocalizedTexts.ReferenceLocale, locale)));
            }
        }

        private static CatalogFinding Wording(
            CatalogFindingKind kind, CatalogView catalog, CatalogRecord record, string key, string message) =>
            new(kind, catalog.Catalog, record.CurrentId, key, Where: string.Empty, message);

        /// <summary>The keys a locale writes twice. gettext keeps the first, so the second is a line the
        /// author wrote and nothing reads — and a tool that edited it would edit the copy nobody sees.</summary>
        private static void Repeated(LocalizedTexts? texts, ICollection<CatalogFinding> found)
        {
            if (texts is null) return;

            foreach (string locale in texts.Locales)
            {
                HashSet<string> seen = new(StringComparer.Ordinal);

                foreach (PoEntry entry in texts.Entries(locale))
                {
                    if (entry.IsHeader || seen.Add(entry.MsgId)) continue;

                    found.Add(new CatalogFinding(
                        CatalogFindingKind.DuplicateKey,
                        Catalog: string.Empty,
                        Record: string.Empty,
                        entry.MsgId,
                        Text(LineFormat, locale, entry.Line),
                        Text(DuplicateKeyFormat, entry.MsgId)));
                }
            }
        }

        // ── names ──────────────────────────────────────────────────────────────────────────────────

        private static string Place(CatalogFile file, JsonPointer at) => Text(PlaceFormat, file.Name, at);

        private static string Targets(IReadOnlyList<ReferenceTarget> targets)
        {
            List<string> named = [];

            foreach (ReferenceTarget target in targets) named.Add(target.ToString());

            return string.Join(ShapeSeparator, named);
        }

        /// <summary>
        /// What the run has said already, for the two facts that belong to the RUN and not to the place
        /// they were met in: a catalog nobody described, and a key no locale words.
        /// <para>Both would otherwise be said once per place. A catalog is pointed at from hundreds of
        /// fields, and a key belongs to the .po files rather than to the records asking after it — two
        /// records written under one id owe one key between them, and the author has one line to write
        /// either way.</para>
        /// <para>A key is taken down where a finding is actually made and not where one is looked for,
        /// which is what keeps a catalog that merely offers the key from spending the saying owed to the
        /// catalog that requires it.</para>
        /// </summary>
        private sealed class Once
        {
            private readonly HashSet<string> _targets = new(StringComparer.Ordinal);
            private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

            public bool Target(string named) => _targets.Add(named);

            public bool Key(string key) => _keys.Add(key);
        }

        /// <summary>What each section of one catalog has been listed under so far, in the order the files
        /// write it, and where the record repeating a name stands. Counted per section and not per catalog:
        /// two sections of one file answer to nothing each other, and a word standing in both is two
        /// records the reader keeps.</summary>
        private sealed class Names
        {
            /// <summary>How many records under one name make it a name written twice.</summary>
            private const int Repeat = 2;

            private readonly List<(string Section, string Id)> _order = [];
            private readonly Dictionary<(string Section, string Id), Listed> _counted = [];

            public void Take(CatalogSchema schema, CatalogRecord record)
            {
                if (!CatalogRecords.Names(schema, record.Schema)) return;
                if (record.CurrentId is not { Length: > 0 } id) return;

                (string Section, string Id) named = (record.Section, id);

                if (!_counted.TryGetValue(named, out Listed? listed))
                {
                    _counted[named] = listed = new Listed();
                    _order.Add(named);
                }

                listed.Written++;

                // The address of the record that repeats the name and not of the one that took it: the
                // first is written where it belongs, and the author is looking for the one too many.
                if (listed.Written == Repeat) listed.Where = Place(record.File, record.Pointer);
            }

            public void Twice(string catalog, ICollection<CatalogFinding> found)
            {
                foreach ((string section, string id) in _order)
                {
                    Listed listed = _counted[(section, id)];

                    if (listed.Where is not { } where) continue;

                    found.Add(new CatalogFinding(
                        CatalogFindingKind.DuplicateId,
                        catalog,
                        id,
                        id,
                        where,
                        Text(DuplicateIdFormat, id, listed.Written, section)));
                }
            }

            /// <summary>One name of one section: how many records answer to it, and where the second of
            /// them stands — null while there is only one, which is a name written once.</summary>
            private sealed class Listed
            {
                public int Written { get; set; }

                public string? Where { get; set; }
            }
        }
    }
}
