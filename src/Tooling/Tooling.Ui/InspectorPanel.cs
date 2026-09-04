namespace Tooling.Ui
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Godot;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Localization;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>One change of the document a panel is showing, made as a whole gesture: on the document's
    /// own history, with the panel kept from redrawing under the hand that made it and drawn again once
    /// it is over. False when nothing was written.</summary>
    public delegate bool RecordGesture(Func<JsonTreeDocument, bool> change);

    /// <summary>
    /// One record, field by field, in the order its schema names them — and, for the values a schema
    /// can vouch for, the control that changes them. Everything a control writes goes into the
    /// document's history, so one key takes it back.
    /// <para>Structure is authored here too: an element added to a list, taken out of it or moved along
    /// it, a key added to a map or taken out, an optional key of a record written or dropped, and the
    /// shape a polymorphic record is written in. All of it goes onto the same history as a typed value,
    /// and every one of them moves the addresses of what stands below — which is why a structural gesture
    /// always redraws, and never from inside the press that asked for it.</para>
    /// <para>The panel is rebuilt whole for every record and after every step through the history. A
    /// record is small, and rebuilding is the one way of showing what the document holds now that cannot
    /// leave a stale row behind.</para>
    /// </summary>
    public partial class InspectorPanel : VBoxContainer
    {
        /// <summary>Width of the name column, so the values of a record line up as one column and not
        /// as a ragged edge following the longest field name.</summary>
        private const int NameWidth = 170;

        /// <summary>How far a nested section steps in from its heading.</summary>
        private const int Indent = 14;

        private const int HeaderFontSize = 15;

        /// <summary>How deep the panel draws before it stops describing and starts quoting. Data this
        /// far down is a structure the author reads as json anyway, and an unbounded walk would let one
        /// record fill the panel with hundreds of rows.</summary>
        private const int MaxDepth = 6;

        /// <summary>How much raw json one row shows before it is cut.</summary>
        private const int RawLimit = 200;

        /// <summary>What one press of a whole-number box's arrow is worth.</summary>
        private const double IntegerStep = 1;

        /// <summary>Where the arrows of a box the schema gave no range stop. Not a bound on the value:
        /// a box without a range accepts anything typed into it, and this is only how far it will walk
        /// on its own.</summary>
        private const double UnboundedLimit = 1_000_000_000;

        /// <summary>What an option button holds when nothing it offers answers for what the file has.</summary>
        private const int NoChoice = -1;

        private const string Ellipsis = "…";

        /// <summary>What stands where the file wrote nothing.</summary>
        private const string Missing = "—";

        private const string EmptyText = "nothing selected";
        private const string GoneText = "the record is no longer in the document";
        private const string NoFieldsText = "the schema names no fields to show for this record";
        private const string UnknownShapeText = "the record is written in a shape the schema does not list";

        private const string HeaderFormat = "{0}   ·   {1}";
        private const string CountFormat = "{0}   ({1})";
        private const string DefaultFormat = "{0}   (default)";
        private const string ReferenceFormat = "{0}   → {1}";
        private const string TranslationFormat = "{0}   ·   “{1}”";
        private const string QuotedFormat = "“{0}”";
        private const string IndexFormat = "[{0}]";

        /// <summary>How "no value at all" is offered when the game reads something in its place: the
        /// author choosing it has to be able to see what leaving the key out actually means.</summary>
        private const string MissingDefaultFormat = "{0}   ({1})";

        /// <summary>The name the shape of a polymorphic record is picked under.</summary>
        private const string FormName = "form";

        /// <summary>What the block holding the record's own wording is called.</summary>
        private const string TextsName = "text";

        /// <summary>How tall a box holding a sentence opens. Fixed rather than grown to fit what is in
        /// it: a description of a few lines would otherwise push the record's own fields off screen, and
        /// the author reads the wording and the numbers of a record together.</summary>
        private const int TextBlockHeight = 68;

        private const string TextHint = "what this key says in this locale; it is written to that locale's .po";
        private const string TextRefusedFormat = "“{0}” could not be written in {1}";
        private const string RenamedKeysFormat = "renamed {0} localization key(s)";
        private const string KeysNotMovedFormat = "keys for “{0}” were not moved: “{1}” is already written";

        /// <summary>What a rename carried: where the name was rewritten, and how much of the wording went
        /// with it.</summary>
        private const string RenamedFormat = "renamed {0} use(s) in {1} file(s); {2} key(s) moved";

        /// <summary>What a rename whose wording would not follow says. Said together with what it did
        /// carry: the references have moved by then, and a line saying only the refusal reads as if the
        /// gesture had done nothing at all.</summary>
        private const string KeysStayedFormat = "{0}; the wording stayed with “{1}”: “{2}” is already written";

        /// <summary>What a rename is called on the status line, in both directions of the history.</summary>
        private const string RenameStepFormat = "rename {0} → {1}";

        private const string AddElementText = "+ element";
        private const string AddKeyText = "+ key";
        private const string AddFieldText = "+ field";
        private const string AddRecordText = "+ record";
        private const string RemoveText = "×";
        private const string UpText = "↑";
        private const string DownText = "↓";

        private const string FormHint = "the shape this record is written in";
        private const string AddElementHint = "add an element at the end of the list";
        private const string RemoveElementHint = "take this element out";
        private const string MoveUpHint = "move this element one place up";
        private const string MoveDownHint = "move this element one place down";
        private const string FirstElementHint = "this is the first element";
        private const string LastElementHint = "this is the last element";
        private const string AddKeyHint = "write this key into the map";
        private const string RemoveKeyHint = "take this key out of the map";
        private const string EveryKeyHint = "every key the schema names is already written";
        private const string AddFieldHint = "write this key into the record";
        private const string RemoveFieldHint = "take this key out; the game reads the default in its place";
        private const string RequiredFieldHint = "the schema requires this key: it can be emptied, not removed";
        private const string AbsentFieldHint = "the record does not hold this key";

        // ── entries of a vocabulary ────────────────────────────────────────────────────────────

        private const string AddRecordHint = "write an entry of this vocabulary here";
        private const string NoTypesHint = "this run knows no type this key may be written as";
        private const string EntryTypeHint =
            "which entry of the vocabulary this is; the keys both types are written with keep their values";

        private const string UnknownTypeFormat = "“{0}” is a type this run does not know; nothing written under it is touched";
        private const string NoTypeText = "this entry names no type, so the game reads nothing here";
        private const string NotAnEntryText = "an entry of the vocabulary is written as an object, and this is not one";
        private const string NothingWrittenText = "nothing is written here yet";

        /// <summary>What is said where the file wrote a lone entry under a key the game reads as a list.
        /// The game reads nothing at all there — a list is what it walks, and an object is not one — so the
        /// row says the file is wrong rather than describing it. The entry is still edited where it
        /// stands, and the press below the block is what puts the key right.</summary>
        private const string LoneEntryText =
            "an object stands here and this key is read as a list: the game reads nothing of this entry, and the press below puts that right";

        private const string MakeListText = "make it a list";

        private const string MakeListHint = "write this entry as a list of one, which is the shape the game reads here";

        /// <summary>What an empty key box says while the schema has nothing to say about the keys.</summary>
        private const string KeyPlaceholder = "key";

        private const string KeyTakenFormat = "“{0}” is already there";
        private const string NoKeyText = "name the key to add";

        private const string FontSizeOverride = "font_size";
        private const string FontColorOverride = "font_color";
        private const string MarginLeftOverride = "margin_left";

        /// <summary>Which control answers for which kind of value — the one place a kind and a widget are
        /// put together. A kind absent from here has no editor and is drawn as text, which is what keeps
        /// objects, lists and maps read-only without a second rule saying so. Free json is read too, unless
        /// a vocabulary answers for the key: those are drawn as the entries they hold.</summary>
        private static readonly Dictionary<FieldKind, Func<FieldEdit, Control>> s_editors = new()
        {
            [FieldKind.String] = TextBox,
            [FieldKind.Reference] = ReferenceBox,
            [FieldKind.LocalizedKey] = LocalizedKey,
            [FieldKind.Integer] = WholeNumber,
            [FieldKind.Number] = Fraction,
            [FieldKind.Boolean] = Flag,
            [FieldKind.Enum] = Member
        };

        private CatalogRecord? _record;
        private JsonTreeDocument? _document;

        /// <summary>The suffixes the open catalog words its records' keys with. Held beside the record
        /// because it is the catalog's answer and not the record's, and the two are handed over
        /// together.</summary>
        private IReadOnlyList<string> _suffixes = [];

        /// <summary>The id the record carried when the last run of keystrokes over it began. The keys of
        /// the .po files are worded from it, so what they have to be named again from is the word that
        /// was there before the author started typing — not the one the file was opened under.</summary>
        private string _idBefore = string.Empty;

        /// <summary>The id of the record standing before this one in its catalog, which is where this
        /// record's own keys are laid down after.</summary>
        private string? _after;

        /// <summary>Where the next key this record lays down goes: after the last key the record itself
        /// has written, or the one before it in the catalog. Walked forward as the panel draws, so a
        /// second line of dialogue lands under the first rather than at the end of the file.</summary>
        private string? _keyAnchor;

        /// <summary>The row naming the record. Held because it is the one thing rewritten without a
        /// redraw: it carries the id, and the id is edited from a box in this very panel.</summary>
        private Label? _heading;

        /// <summary>Which build of the panel the controls on screen belong to. A control that outlived
        /// its build speaks for nothing: a field commits as it loses focus, and it loses focus while the
        /// panel is being torn down, so a stale box would write its old text back over an undo.</summary>
        private object _build = new();

        /// <summary>A control of this panel is writing. The document tells everyone it changed, this
        /// panel included, and redrawing on its own write would tear down the control under the hand
        /// that is typing into it.</summary>
        private bool _writing;

        /// <summary>What a gesture came to when the record itself cannot show it: a key already written,
        /// a key nobody named. The panel has no line of its own to say it on, and the tool has one that
        /// says what the run last did.</summary>
        public event Action<string>? Said;

        /// <summary>The ids the run knows, by the catalog writing them. A field that points at a record
        /// of another catalog is answered from here — offered as a list to pick from, and marked when the
        /// word standing in it answers to nothing. Null while nothing has been read, which leaves a
        /// reference the plain box it always was.</summary>
        public ReferenceIndex? References { get; set; }

        /// <summary>Where the run writes each id, which is what a rename of one has to carry with it: the
        /// record's new name is written into every field of every catalog holding the old one, as part of
        /// the same gesture. Null while the run has none, which leaves a rename the record's own file and
        /// its wording — everything naming it goes on naming what it was called.</summary>
        public ReferenceUses? Uses { get; set; }

        /// <summary>The wording of the game, in every locale at once. Null while the run has none — the
        /// block of text is then not drawn at all, because a box that writes nowhere is worse than no
        /// box.</summary>
        public LocalizedTexts? Texts { get; set; }

        /// <summary>
        /// Which fields hold entries of a vocabulary — a set of types named by a word the entry carries —
        /// rather than a value of the catalog's own. The panel asks about a field and is answered with the
        /// types, the key naming them and whether the field holds one entry or a list; what the words mean
        /// is the host's business and never this panel's.
        /// <para>Null, or an answer of null, leaves the field the raw json it always was: a field the run
        /// has no vocabulary for is one the tool cannot vouch for writing.</para>
        /// </summary>
        public Func<FieldSchema, VocabularyBinding?>? Vocabularies { get; set; }

        /// <summary>
        /// Which records are drawn by a form of their own instead of by the general reading of their
        /// schema. The panel asks about a record and is answered with the control drawing it, or with
        /// null — a record no form answers for is drawn field by field, the way every record always was.
        /// <para>What a form knows about the catalog it draws is the host's business. This panel keeps
        /// exactly one thing back for itself: the row naming the record, which belongs to the catalog
        /// listing it and not to the shape of what it holds.</para>
        /// </summary>
        public Func<CatalogRecord, Control?>? Forms { get; set; }

        /// <summary>
        /// What the host does about the record's wording once a gesture of this panel is over — a change
        /// of its shape, or a run of keystrokes just sealed. Called after the panel has done its own,
        /// so the two never write over each other.
        /// <para>The block of text above draws the keys a catalog words from a record's id, and the
        /// rename beside it moves those. A catalog wording its keys from the STRUCTURE a record stands in
        /// — the node a line belongs to, the option's own name — cannot say so as a suffix of one id, and
        /// this is where it says it instead. Null leaves a record's wording exactly what its own keys
        /// say, which is what it always was.</para>
        /// <para>Answers whether it wrote anything, and the panel is drawn again only where it did: every
        /// box left, every box ticked and every id picked ends a gesture, and a panel torn down and built
        /// again on each of them is one the author cannot keep his place in.</para>
        /// </summary>
        public Func<bool>? Settled { get; set; }

        /// <summary>How anything drawn inside this panel changes the document under it: one gesture, one
        /// step of the history, and the panel drawn again once the gesture is over rather than under the
        /// hand that made it.</summary>
        public RecordGesture Gestures => Restructure;

        /// <summary>Draws the record, or says why there is nothing to draw.
        /// <paramref name="suffixes"/> is what its catalog words the record's localization keys with, and
        /// <paramref name="after"/> the id of the record standing before it — the place a key this record
        /// lays down belongs after.</summary>
        public void Rebuild(CatalogRecord? record, IReadOnlyList<string>? suffixes = null, string? after = null)
        {
            _record = record;
            _suffixes = suffixes ?? [];
            _after = after;

            Follow(record?.File.Document);
            DrawRecord();
        }

        private static Label Header(string text)
        {
            var label = new Label { Text = text };
            label.AddThemeFontSizeOverride(FontSizeOverride, HeaderFontSize);

            return label;
        }

        /// <summary>A heading with an indented body under it, and the body is where the caller keeps
        /// drawing. A nested record reads as a block that way without the panel having to know how deep
        /// it already is. What may be done to the block as a whole hangs off its heading, where the block
        /// is named — a button under the last row of a long list would be answering about nothing
        /// visible.</summary>
        private static Node Section(Node parent, string name, Control? actions, string? documentation = null)
        {
            Label header = Meaning(Header(name), documentation);

            parent.AddChild(actions is null ? header : Headed(header, actions));

            return Indented(parent);
        }

        /// <summary>Hangs what a thing means off the label naming it. A label ignores the mouse, and a
        /// tooltip is only ever shown for a control the mouse can land on; a label with nothing to say goes
        /// on ignoring it.</summary>
        private static Label Meaning(Label label, string? documentation)
        {
            if (documentation is not { Length: > 0 }) return label;

            label.TooltipText = documentation;
            label.MouseFilter = MouseFilterEnum.Pass;

            return label;
        }

        /// <summary>A block stepped in under the row above it, with nothing heading it: what it holds
        /// belongs to that row, and naming it again would be the same word written twice.</summary>
        private static Node Indented(Node parent)
        {
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride(MarginLeftOverride, Indent);

            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            margin.AddChild(body);
            parent.AddChild(margin);

            return body;
        }

        /// <summary>A heading with what may be done to what it names beside it.</summary>
        private static Control Headed(Label header, Control actions)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            header.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            row.AddChild(header);
            row.AddChild(actions);

            return row;
        }

        /// <summary>One field as a row. What the field means hangs off its name as a tooltip: the panel
        /// is read down the value column, and a sentence per row printed in full would push the values
        /// apart until the record no longer reads as one thing.</summary>
        private static Control Row(string name, Control value, string? documentation, Control? actions)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            var label = new Label
            {
                Text = name,
                CustomMinimumSize = new Vector2(NameWidth, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            row.AddChild(Meaning(label, documentation));
            row.AddChild(value);

            if (actions is not null) row.AddChild(actions);

            return row;
        }

        private static Label ValueLabel(string text) => new()
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        /// <summary>Whether a field is the one whose value names the record's shape. It is drawn once, as
        /// the picker: a second control over the same key would be a second answer to which shape this
        /// is, and the two would take turns overwriting each other.</summary>
        private static bool NamesTheForm(RecordSchema schema, FieldSchema field) =>
            schema.Variants is { Discriminator: { } named }
            && string.Equals(field.JsonName, named, StringComparison.Ordinal);

        /// <summary>One field as a line of text. A value the file does not hold is named by the default
        /// the game will read instead, which is the answer to "what happens if I leave this out".</summary>
        private string Scalar(FieldSchema field, JToken? value)
        {
            if (value is null || value.Type == JTokenType.Null)
                return field.Default is { } fallback ? Text(DefaultFormat, fallback) : Missing;

            // A structure standing where a value was expected, or one too deep to keep describing: the
            // json itself is the honest answer, cut where a row stops being readable.
            if (value is JObject or JArray || field.Kind == FieldKind.Any) return Raw(value);

            string written = JsonScalars.Written(value);

            if (written.Length == 0) return Missing;

            return field.Kind switch
            {
                FieldKind.Reference when field.RefTargets.Count > 0 =>
                    Text(ReferenceFormat, written, ReferencePicker.Targets(field.RefTargets)),
                FieldKind.LocalizedKey => Translated(written),
                _ => written
            };
        }

        /// <summary>A localization key together with the text it stands for. A key nothing translates
        /// answers with itself, and the panel says it once: the author is reading whether the key has
        /// been written yet, and a line repeating it says that as clearly as a line saying nothing.</summary>
        private string Translated(string key)
        {
            string translation = Translation(key);

            return translation.Length == 0 ? key : Text(TranslationFormat, key, translation);
        }

        /// <summary>
        /// The text a key stands for, or nothing when it stands for itself — a key nobody has written a
        /// translation for yet.
        /// <para>Read out of the files the run is editing whenever it has them, and out of the engine only
        /// when it has not. The engine loaded its translations when the tool started and in one locale:
        /// asked after an edit it answers with the word that was there an hour ago, and asked at all it
        /// answers in whichever locale the tool happens to be running in.</para>
        /// </summary>
        private string Translation(string key)
        {
            if (key.Length == 0) return string.Empty;

            if (Texts is { } texts)
                return texts.Locales.Select(locale => texts.Read(locale, key)).FirstOrDefault(said => said is { Length: > 0 })
                       ?? string.Empty;

            string translation = TranslationServer.Translate(key).ToString();

            return string.Equals(translation, key, StringComparison.Ordinal) ? string.Empty : translation;
        }

        private static string Raw(JToken token)
        {
            string json = token.ToString(Formatting.None);

            return json.Length <= RawLimit ? json : json[..RawLimit] + Ellipsis;
        }

        private static JToken? Value(JToken token, string name) =>
            token is JObject holder && holder.TryGetValue(name, StringComparison.Ordinal, out JToken? value) ? value : null;

        // ── controls ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A box of text, which is what a name, a localization key and a reference all are. A reference
        /// says which catalogs may answer it rather than offering their ids: the record being pointed at
        /// lives in a catalog this panel has not read, and a list built from the one it has read would
        /// be an offer of the wrong ids.
        /// </summary>
        private static LineEdit Box(FieldEdit edit)
        {
            string hint = Hint(edit.Field);

            var box = new LineEdit
            {
                Text = edit.Written,
                PlaceholderText = hint,
                TooltipText = hint,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            box.TextChanged += typed => edit.Write(new JValue(typed));

            // Leaving the box ends the run of keystrokes it was taking, so the next field edited is a
            // step of its own and not the tail of this one.
            box.FocusExited += edit.Seal;

            return box;
        }

        /// <summary>What an empty box says instead of standing blank: where a reference may point,
        /// or the value the game reads while the key is absent.</summary>
        private static string Hint(FieldSchema field)
        {
            if (field.Kind == FieldKind.Reference && field.RefTargets.Count > 0)
                return ReferencePicker.Targets(field.RefTargets);

            return DefaultText(field.Default);
        }

        private static Control TextBox(FieldEdit edit) => Box(edit);

        /// <summary>
        /// A box of text with the catalogs it points into behind it: the word may be typed, and it may be
        /// picked out of the ids those catalogs actually write. A word answering to none of them is
        /// marked where it stands — a reference nobody has is a record the game drops on load, and the
        /// file gives no sign of it until then.
        /// </summary>
        private static Control ReferenceBox(FieldEdit edit)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            LineEdit box = Box(edit);
            var pick = new Button
            {
                Text = ReferencePicker.PickText,
                TooltipText = ReferencePicker.PickHint,
                Disabled = !edit.Offered
            };

            void Judge(string written) => Judged(edit, box, written);

            // Judged as it is typed: the author is asking exactly whether the word he is writing answers
            // to anything, and a mark that only arrived on the next record would answer about another.
            Judge(edit.Written);
            box.TextChanged += Judge;

            pick.Pressed += () => edit.Pick(box, id =>
            {
                if (!edit.Write(new JValue(id))) return;

                box.Text = id;
                edit.Seal();
                Judge(id);
            });

            row.AddChild(box);
            row.AddChild(pick);

            return row;
        }

        /// <summary>Marks the word standing in a reference, and says on it what is wrong. The judgement
        /// itself is the picker's, so that what is offered and what is marked are one answer.</summary>
        private static void Judged(FieldEdit edit, LineEdit box, string written)
        {
            box.RemoveThemeColorOverride(FontColorOverride);

            // A field that names no catalog at all keeps the box it always was, hint and all: there is
            // nothing to check it against and nothing to offer instead.
            if (!edit.Offered) return;

            ReferenceVerdict verdict = ReferencePicker.Judge(edit.References, edit.Field.RefTargets, written, edit.Field.AllowEmpty);

            box.TooltipText = verdict.Say;

            if (verdict.Broken) box.AddThemeColorOverride(FontColorOverride, ReferencePicker.BrokenReference);
        }

        /// <summary>A key and, beside it, the text it stands for now. The translation follows the
        /// keystrokes: an author typing a key is asking exactly that question, and an answer that only
        /// arrived on the next selection would be answering about the key before it.</summary>
        private static Control LocalizedKey(FieldEdit edit)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var translation = ValueLabel(string.Empty);
            LineEdit box = Box(edit);

            void Show(string key)
            {
                string text = edit.Translation(key);

                translation.Text = text.Length == 0 ? string.Empty : Text(QuotedFormat, text);
            }

            Show(edit.Written);
            box.TextChanged += Show;

            row.AddChild(box);
            row.AddChild(translation);

            return row;
        }

        /// <summary>
        /// A whole number in a spin box. The range the schema declares becomes the range of the box; a
        /// field without one is left open at both ends — the arrows stop somewhere readable, and a
        /// number typed past that is accepted, because the schema saying nothing about a bound is not
        /// the same as it declaring one.
        /// </summary>
        private static Control WholeNumber(FieldEdit edit)
        {
            NumericRange? range = edit.Field.Range;
            double opening = Opening(edit);

            // A range the file already stands outside of does not become the box's own: a box that
            // clamped it would rewrite a number the author never touched, and the file being wrong is
            // exactly what the author opened it to see.
            bool free = range is not { } declared || !declared.Contains(opening);

            var box = new SpinBox
            {
                Step = IntegerStep,
                Rounded = true,
                MinValue = range?.Min ?? -UnboundedLimit,
                MaxValue = range?.Max ?? UnboundedLimit,
                AllowLesser = free,
                AllowGreater = free,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            // The value before the signal, so that opening on a default writes nothing: an absent key is
            // written by the author changing it, not by the panel having drawn it.
            box.Value = opening;
            box.ValueChanged += number => edit.Write(JsonScalars.Number(edit.Value, number, whole: true));
            box.GetLineEdit().FocusExited += edit.Seal;

            return box;
        }

        /// <summary>
        /// A fraction as text. A spin box cannot stand here: it snaps whatever it is given to its own
        /// step, so a file holding 0.12345 would be shown as another number the moment it was looked at,
        /// and it has no way to show an absent key as absent — it always holds a number, and 0 is a
        /// value the author never wrote.
        /// <para>Refusals are visible rather than swallowed: text that is not a number puts back what
        /// the file holds, and an empty box is the field being left alone, since clearing a number is
        /// not the same as choosing one.</para>
        /// </summary>
        private static Control Fraction(FieldEdit edit)
        {
            string hint = DefaultText(edit.Field.Default);

            var box = new LineEdit
            {
                Text = edit.Written,
                PlaceholderText = hint,
                TooltipText = hint,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            void Commit(string typed)
            {
                string wanted = typed.Trim();

                if (wanted.Length > 0 && Parsed(wanted) is { } number)
                    edit.Write(JsonScalars.Number(edit.Value, number, whole: false));
                else box.Text = edit.Written;
            }

            box.TextSubmitted += Commit;
            box.FocusExited += () =>
            {
                Commit(box.Text);
                edit.Seal();
            };

            return box;
        }

        /// <summary>The number a text stands for, read in the invariant culture because that is the one
        /// the file writes in. Null when the text is not a number json can hold — how it is spelled has
        /// no say in what kind of number is written back, which is the file's own business.</summary>
        private static double? Parsed(string text) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            && double.IsFinite(number)
                ? number
                : null;

        private static Control Flag(FieldEdit edit)
        {
            var box = new CheckBox { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            // Set before the signal is connected: a box drawn as false on an absent key would otherwise
            // write that false into the file for having been drawn.
            box.ButtonPressed = edit.Value is { Type: JTokenType.Boolean } written
                ? written.Value<bool>()
                : edit.Field.Default is true;

            // Sealed as part of the click: a tick is a whole gesture, so two ticks of one box are two
            // steps back and not one merged into the other.
            box.Toggled += pressed =>
            {
                edit.Write(new JValue(pressed));
                edit.Seal();
            };

            return box;
        }

        /// <summary>
        /// One member of an enum. A value the enum does not list stays offered: the tool never silently
        /// rewrites what it does not understand, and an author who cannot see the wrong word cannot fix
        /// it.
        /// <para>"No value at all" is an entry like any other, named by what the game reads in its
        /// place. It is offered wherever the file may legally lack the key, and also on a required key
        /// the file lacks anyway — a picker that showed nothing selected would leave the author reading
        /// a blank box with no way to learn what the game does with it. It is not offered on an element
        /// of a list or a value of a map: leaving those out is taking the element out, which is what the
        /// row's own button does, and one gesture with two controls is one of them going unnoticed.</para>
        /// </summary>
        private static Control Member(FieldEdit edit)
        {
            var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            string written = edit.Written;
            List<string> members = [.. edit.Field.EnumValues];

            if (written.Length > 0 && !members.Contains(written, StringComparer.Ordinal)) members.Add(written);

            // The id of "none" is one past the last member rather than a fixed sentinel: an enum is as
            // long as the game makes it, and a constant chosen today is a collision the day it grows
            // past it. Negative ids cannot stand in — Godot substitutes the item index for those.
            int none = members.Count;
            bool absence = edit.Removable && (!edit.Field.Required || written.Length == 0);

            if (absence) picker.AddItem(Absent(edit.Field), none);

            for (int index = 0; index < members.Count; index++) picker.AddItem(members[index], index);

            int at = members.IndexOf(written);

            // Nothing selected where nothing on offer answers for what the file holds: an element written
            // as no member at all is not one, and standing on the first member would be a claim.
            picker.Selected = at >= 0 ? picker.GetItemIndex(at) : absence ? picker.GetItemIndex(none) : NoChoice;

            picker.ItemSelected += index =>
            {
                int id = picker.GetItemId((int)index);
                bool wrote = id == none ? edit.Erase() : edit.Write(new JValue(members[id]));

                edit.Seal();

                // Redrawn because a member can be what decides the record's shape: the fields under it
                // are another set now, and the panel showing the old ones is showing a record nobody has.
                if (wrote) edit.Redraw();
            };

            return picker;
        }

        /// <summary>How the entry standing for an absent key is named: by the value the game reads
        /// instead, when there is one to name.</summary>
        private static string Absent(FieldSchema field) =>
            DefaultText(field.Default) is { Length: > 0 } fallback
                ? Text(MissingDefaultFormat, Missing, fallback)
                : Missing;

        /// <summary>The number a box opens on: what the file holds, or the default the game reads while
        /// the key is absent, or nothing at all.</summary>
        private static double Opening(FieldEdit edit)
        {
            string written = edit.Written.Length > 0 ? edit.Written : DefaultText(edit.Field.Default);

            return double.TryParse(written, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                ? number
                : 0;
        }

        /// <summary>A schema's default as the file would spell it. Read through the invariant culture:
        /// a fraction shown with the machine's comma is not the number the author would type back.</summary>
        private static string DefaultText(object? value) =>
            value is null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

        // ── drawing ────────────────────────────────────────────────────────────────────────────

        /// <summary>Follows one document at a time, so the panel hears about the record it is showing and
        /// about nothing else.</summary>
        private void Follow(JsonTreeDocument? document) =>
            _document = PanelParts.Following(_document, document, OnDocumentChanged);

        /// <summary>The document changed under the panel — an undo, a redo, or an edit made somewhere
        /// else. Everything on screen was copied out of the tree, and a control still showing what was
        /// typed a moment ago is the exact confusion undo exists to prevent.</summary>
        private void OnDocumentChanged(JsonPointer pointer)
        {
            // A control of this panel writing is not news to the panel: redrawing here would tear down
            // the very box the author is typing into, one keystroke in. The heading is written again all
            // the same — it names the record by its id, and the id is one of the fields being typed in.
            if (_writing)
            {
                NameRecord();
                return;
            }

            DrawRecord();
        }

        private void DrawRecord()
        {
            // Every control of the previous build stops speaking here: whatever a torn-down box does on
            // its way out is written to nothing.
            _build = new object();
            _heading = null;

            this.DropChildren();

            if (_record is not { } record)
            {
                AddChild(new Label { Text = EmptyText });
                return;
            }

            if (record.Token is not { } token)
            {
                AddChild(new Label { Text = GoneText });
                return;
            }

            _heading = Header(string.Empty);
            AddChild(_heading);
            NameRecord();

            _idBefore = record.CurrentId;

            // The keys of the record itself where it has written any, and otherwise those of the record
            // before it: whatever a key laid down further down this panel belongs under.
            _keyAnchor = Texts?.AnchorOf(record.CurrentId, _suffixes) ?? Texts?.AnchorOf(_after, _suffixes);

            AddTexts(record);

            // A record with a form of its own is handed over whole, keeping only the row naming it: the
            // form answers for what the record HOLDS, while the id is what its catalog lists it under —
            // and a catalog whose records could no longer be renamed would have lost that by being drawn
            // better.
            if (Forms?.Invoke(record) is { } form)
            {
                AddId(record, token);
                AddChild(form);

                return;
            }

            int drawn = GetChildCount();
            AddRecord(this, record.Schema, token, record.Pointer, depth: 0);

            // A record whose every field is machinery, or none at all: the panel says so rather than
            // standing empty under a heading, which reads as the tool having failed to draw it.
            if (GetChildCount() == drawn) AddChild(new Label { Text = NoFieldsText });
        }

        /// <summary>The row naming the record, drawn ahead of a form of its own. Drawn by the same
        /// machinery every other field is, so the word a key of a section points at — a fraction, a kind
        /// of foe, an npc — is picked here exactly as it is picked anywhere else. Nothing at all where the
        /// record's schema names no id field.</summary>
        private void AddId(CatalogRecord record, JToken token)
        {
            if (record.Schema.IdField is not { } id) return;

            foreach (FieldSchema field in record.Schema.Fields)
            {
                if (!string.Equals(field.JsonName, id, StringComparison.Ordinal)) continue;

                JsonPointer at = record.Pointer.Append(id);
                JToken? value = Value(token, id);

                AddField(this, field, value, id, at, named: true, depth: 0, FieldGesture(field, value, at));

                return;
            }
        }

        /// <summary>Writes the heading: the record's id as the document holds it now, and the shape it is
        /// drawn by. Asked again on every change because the id is a field the author may retype, and a
        /// heading naming the word the file held when it was opened would be naming another record.</summary>
        private void NameRecord()
        {
            if (_heading is null || _record is not { } record) return;

            _heading.Text = Text(HeaderFormat, record.CurrentId, record.Schema.TypeName);
        }

        /// <summary>Draws the record again once the gesture that asked for it is over. A control commits
        /// as it loses focus, and it loses focus while the panel is being torn down — redrawing from
        /// inside that would leave two redraws walking the same children.</summary>
        private void RebuildLater() => Callable.From(DrawRecord).CallDeferred();

        private void AddRecord(Node parent, RecordSchema schema, JToken token, JsonPointer at, int depth)
        {
            VariantSchema? worn = Wearing(schema, token);
            IReadOnlyList<FieldSchema> written = Drawn(schema, worn);

            // Said before anything is drawn: what follows is everything the record declares and not the
            // shape it claims to be, and the author has to see that the file names something the game will
            // not recognise.
            if (schema.Variants is not null && worn is null) parent.AddChild(new Label { Text = UnknownShapeText });

            // With no field naming the shapes the presence of a key is what names them, and the picker has
            // no row of the record's own to stand in: it heads the record instead.
            if (schema.Variants is { Discriminator: null } nameless) AddForm(parent, nameless, worn, token, at);

            foreach (FieldSchema field in written)
            {
                if (field.Hidden) continue;

                if (NamesTheForm(schema, field) && schema.Variants is { } named)
                {
                    AddForm(parent, named, worn, token, at);
                    continue;
                }

                JsonPointer key = at.Append(field.JsonName);
                JToken? value = Value(token, field.JsonName);

                AddField(parent, field, value, field.JsonName, key, named: true, depth, FieldGesture(field, value, key));
            }

            if (token is JObject holder && NewField(written, schema, holder, at) is { } add) parent.AddChild(add);
        }

        /// <summary>The shape a polymorphic record is written in, or null where it wears none the schema
        /// lists and where it takes only the one shape it was declared with.</summary>
        private static VariantSchema? Wearing(RecordSchema schema, JToken token) =>
            schema.Variants is { } variants ? RecordTemplates.Worn(variants, token) : null;

        /// <summary>
        /// The fields a record is drawn by. A record of one shape is drawn by its own; a record of several
        /// by the fields the shape it wears brings AND by those of the record itself that no shape claims
        /// — the id its catalog lists it under, the inversion every predicate shares — in the order the
        /// file is written in. A key another shape is known by is that shape's alone: written here it
        /// would answer the question the picker asks, and twice over.
        /// <para>A record wearing no shape the schema lists is drawn by everything it declares: what the
        /// file holds has to be readable, and a key hidden on the strength of a shape nobody wears would
        /// be a key the author cannot see and cannot correct.</para>
        /// </summary>
        private static IReadOnlyList<FieldSchema> Drawn(RecordSchema schema, VariantSchema? worn)
        {
            if (schema.Variants is not { } variants || worn is null) return schema.Fields;

            HashSet<string> elsewhere = Elsewhere(variants, worn);

            return [.. RecordFieldOrder.Merged(schema, [worn.Record], Brought).Where(field => !elsewhere.Contains(field.JsonName))];
        }

        /// <summary>The names the shapes this record is NOT written in are known by, less everything the
        /// shape it wears writes too: a key two shapes share belongs to the one standing here.</summary>
        private static HashSet<string> Elsewhere(VariantSet variants, VariantSchema worn)
        {
            HashSet<string> names = new(StringComparer.Ordinal);

            foreach (VariantSchema variant in variants.Variants)
                if (!ReferenceEquals(variant, worn))
                    foreach (FieldSchema field in variant.Record.Fields)
                        names.Add(field.JsonName);

            foreach (FieldSchema field in worn.Record.Fields) names.Remove(field.JsonName);

            return names;
        }

        /// <summary>What is drawn where the record and the shape it wears both name a key: the shape's,
        /// which is the narrower answer — what the record leaves optional for every shape at once is what
        /// one shape requires.</summary>
        private static FieldSchema Brought(FieldSchema placed, FieldSchema brought) => brought;

        /// <summary>The row the shape of a polymorphic record is picked in, standing where the field
        /// naming the shape would have. Nothing at all while the panel has no document to write to, or
        /// where what stands here is not a record: a picker over neither writes nowhere.</summary>
        private void AddForm(Node parent, VariantSet variants, VariantSchema? worn, JToken token, JsonPointer at)
        {
            if (_document is null || token is not JObject) return;

            parent.AddChild(Row(FormName, FormPicker(variants, worn, at), FormHint, actions: null));
        }

        /// <summary>
        /// One field, drawn by what it holds. <paramref name="named"/> is what tells a field of a record
        /// from an element of a list or an entry of a map: only the first has a key of its own that may
        /// be left out of the file, so only the first is offered "no value at all".
        /// <para><paramref name="actions"/> is what may be done to the value where it stands — taken out,
        /// moved — drawn beside the row or beside the heading of the block, whichever the value turns
        /// out to need.</para>
        /// </summary>
        private void AddField(Node parent, FieldSchema field, JToken? value, string name, JsonPointer at,
            bool named, int depth, Control? actions)
        {
            // Before the value is asked about at all: a key field the file does not hold yet is still a key
            // field, and the boxes under it are how the author writes the first word of it. A row that only
            // became editable on the second visit would read as the tool having missed the first.
            if (depth < MaxDepth && Keyed(field, value))
            {
                AddKeyed(parent, field, value, name, at, named, actions);
                return;
            }

            // Before the value is asked about too: a key of a vocabulary the file does not hold yet is
            // still one, and the row under it is how the author writes the first entry into it.
            if (depth < MaxDepth && _document is not null && Vocabularies?.Invoke(field) is { } vocabulary)
            {
                AddVocabulary(parent, field, value, name, at, vocabulary, depth, actions);
                return;
            }

            if (depth >= MaxDepth || value is null || value.Type == JTokenType.Null)
            {
                parent.AddChild(Row(name, Editor(field, value, at, named), field.Documentation, actions));
                return;
            }

            switch (field.Kind)
            {
                case FieldKind.Object when field.Record is { } record && value is JObject:
                    AddRecord(Section(parent, name, actions), record, value, at, depth + 1);
                    break;

                case FieldKind.Array when field.Item is { } item && value is JArray array:
                    Node elements = Section(parent, Text(CountFormat, name, array.Count), actions);

                    for (int index = 0; index < array.Count; index++)
                        AddField(elements, item, array[index], Text(IndexFormat, index), at.Append(index),
                            named: false, depth + 1, ElementGestures(at, index, array.Count));

                    elements.AddChild(NewElement(item, at, array.Count));
                    break;

                case FieldKind.Dictionary when field.Item is { } item && value is JObject map:
                    Node pairs = Section(parent, Text(CountFormat, name, map.Count), actions);

                    foreach (JProperty pair in map.Properties())
                        AddField(pairs, item, pair.Value, pair.Name, at.Append(pair.Name), named: false, depth + 1,
                            Gesture(RemoveText, RemoveKeyHint, enabled: true, document => document.Remove(at.Append(pair.Name))));

                    pairs.AddChild(NewKey(field, item, map, at));
                    break;

                default:
                    parent.AddChild(Row(name, Editor(field, value, at, named), field.Documentation, actions));
                    break;
            }
        }

        /// <summary>The control a field is changed through, or the text it is read as. A value the panel
        /// will not take responsibility for writing is read: a kind no control answers for, and a
        /// structure standing where the schema expected a value — a box over that last one would write a
        /// word across whatever the author actually wrote there.</summary>
        private Control Editor(FieldSchema field, JToken? value, JsonPointer at, bool named)
        {
            if (_document is null || value is JObject or JArray) return ValueLabel(Scalar(field, value));

            return s_editors.TryGetValue(field.Kind, out Func<FieldEdit, Control>? build)
                ? build(new FieldEdit(this, _build, field, value, at, named))
                : ValueLabel(Scalar(field, value));
        }

        // ── vocabularies ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A field written from a vocabulary, drawn as the entries it holds: a block, one entry per row of
        /// it, and under them the row that writes another. An entry standing where the game reads a list,
        /// and a value that is neither, are said out loud rather than redrawn as something they are not.
        /// </summary>
        private void AddVocabulary(Node parent, FieldSchema field, JToken? value, string name, JsonPointer at,
            VocabularyBinding binding, int depth, Control? actions)
        {
            string? meaning = field.Documentation;

            if (binding.List && value is JArray written)
            {
                Node entries = Section(parent, Text(CountFormat, name, written.Count), actions, meaning);

                for (int index = 0; index < written.Count; index++)
                    AddEntry(entries, binding, written[index], Text(IndexFormat, index), at.Append(index),
                        depth + 1, ElementGestures(at, index, written.Count));

                entries.AddChild(NewEntry(binding, (document, blank) => document.Insert(at, written.Count, blank)));
                return;
            }

            // One entry standing where the game reads a list. It is edited where it is; a second one needs
            // a list to stand in, and the press under the block is what makes the room for it.
            if (binding.List && value is JObject)
            {
                Node lone = Section(parent, name, actions, meaning);
                Button list = Gesture(MakeListText, MakeListHint, enabled: true, document => TypedRecords.AsList(document, at));

                list.SizeFlagsHorizontal = SizeFlags.ExpandFill;

                lone.AddChild(new Label { Text = LoneEntryText });
                AddEntry(lone, binding, value, Text(IndexFormat, 0), at, depth + 1, actions: null);
                lone.AddChild(list);

                return;
            }

            if (!binding.List && value is JObject)
            {
                AddEntry(parent, binding, value, name, at, depth + 1, actions, meaning);
                return;
            }

            // Anything else the file wrote here is read as the json it is and left alone: a row offering to
            // write an entry over it would lose what the author actually put there.
            if (value is not null && value.Type != JTokenType.Null)
            {
                parent.AddChild(Row(name, ValueLabel(Raw(value)), NotAnEntryText, actions));
                return;
            }

            Node empty = Section(parent, name, actions, meaning);

            empty.AddChild(new Label { Text = NothingWrittenText });
            empty.AddChild(NewEntry(binding, (document, blank) =>
                document.Put(at, binding.List ? new JArray(blank) : blank)));
        }

        /// <summary>One entry of a vocabulary: the word naming its type, and under it the keys that type is
        /// written with — the same fields, and so the same controls, a record of a catalog is drawn by. A
        /// type holding entries of its own is answered by the very same block one level down.</summary>
        private void AddEntry(Node parent, VocabularyBinding binding, JToken? value, string name, JsonPointer at,
            int depth, Control? actions, string? documentation = null)
        {
            if (value is not JObject holder)
            {
                parent.AddChild(Row(name, ValueLabel(value is null ? Missing : Raw(value)), NotAnEntryText, actions));
                return;
            }

            Node body = Section(parent, name, actions, documentation);
            string standing = TypedRecords.Standing(binding, holder);
            RecordSchema? worn = TypedRecords.Worn(binding, holder);

            body.AddChild(Row(binding.TypeKey, TypePicker(binding, standing, at), EntryTypeHint, actions: null));

            if (standing.Length == 0) body.AddChild(new Label { Text = NoTypeText });
            else if (worn is null) body.AddChild(new Label { Text = Text(UnknownTypeFormat, standing) });

            if (worn is { } type) AddRecord(body, type, holder, at, depth);
            else AddUnknownKeys(body, holder, binding.TypeKey);
        }

        /// <summary>The keys of an entry no known type answers for, read as the json they hold. Nothing is
        /// offered over them and nothing is dropped: the run cannot say what any of them means, and an
        /// author who cannot see them cannot tell the entry came through whole.</summary>
        private static void AddUnknownKeys(Node parent, JObject holder, string typeKey)
        {
            foreach (JProperty pair in holder.Properties())
                if (!string.Equals(pair.Name, typeKey, StringComparison.Ordinal))
                    parent.AddChild(Row(pair.Name, ValueLabel(Raw(pair.Value)), documentation: null, actions: null));
        }

        /// <summary>The type an entry is written as, picked out of the vocabulary. A word the run does not
        /// know stays on offer where the file wrote one — the tool never quietly rewrites what it does not
        /// understand — and nothing is selected where the entry names no type at all.</summary>
        private Control TypePicker(VocabularyBinding binding, string standing, JsonPointer at)
        {
            var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = EntryTypeHint };
            object build = _build;
            List<string> types = [.. binding.Types.Select(type => type.TypeName)];

            if (standing.Length > 0 && !types.Contains(standing, StringComparer.Ordinal)) types.Add(standing);

            foreach (string type in types) picker.AddItem(type);

            picker.Selected = standing.Length == 0 ? NoChoice : types.IndexOf(standing);

            picker.ItemSelected += index =>
            {
                if (Stale(build)) return;

                string chosen = types[(int)index];

                if (!string.Equals(chosen, standing, StringComparison.Ordinal))
                    Restructure(document => TypedRecords.Switch(document, at, binding, chosen));
            };

            return picker;
        }

        /// <summary>The row that writes an entry: the type it is to be written as, and the press that lays
        /// the blank of that type wherever the caller puts it.</summary>
        private Control NewEntry(VocabularyBinding binding, Func<JsonTreeDocument, JToken, bool> lay)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            bool any = binding.Types.Count > 0;
            string hint = any ? AddRecordHint : NoTypesHint;

            var picker = new OptionButton
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Disabled = !any,
                TooltipText = hint
            };

            foreach (RecordSchema type in binding.Types) picker.AddItem(type.TypeName);

            picker.Selected = any ? 0 : NoChoice;

            row.AddChild(picker);
            row.AddChild(Gesture(AddRecordText, hint, any, document =>
                picker.Selected >= 0
                && picker.Selected < binding.Types.Count
                && lay(document, TypedRecords.Blank(binding, binding.Types[picker.Selected].TypeName))));

            return row;
        }

        // ── text ───────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The record's own wording, in every locale, under the keys its catalog words from the id: the
        /// first thing about a record its author reads, so the first thing the panel draws.
        /// <para>Nothing at all where the run has no .po files, where the catalog words no keys from an
        /// id, or where the record carries none — there is no key to write under in any of the three.</para>
        /// </summary>
        private void AddTexts(CatalogRecord record)
        {
            if (Texts is not { } texts) return;

            List<LocalizedTextKey> family = [.. LocalizedTexts.Keys(record.CurrentId, _suffixes, texts.AnchorOf(_after, _suffixes))];

            if (family.Count == 0) return;

            Node body = Section(this, TextsName, actions: null);

            // A heading per key, because the key is what the author checks: the name of a record and its
            // description are two keys and reading which box belongs to which is the whole question.
            for (int index = 0; index < family.Count; index++)
                AddPair(Section(body, family[index].Key, actions: null),
                    new TextKey(family, index, block: family[index].Suffix.Length > 0));
        }

        /// <summary>Whether the field is a key the author writes out in full and this run can write the
        /// text under it. A field whose suffix is not empty has its key worded from the record's id — the
        /// block above already draws that one, and drawing it twice would be two boxes over one key.</summary>
        private bool Keyed(FieldSchema field, JToken? value) =>
            field.Kind == FieldKind.LocalizedKey
            && field.LocalizationSuffix is { Length: 0 }
            && Texts is not null
            && _document is not null
            && value is null or JValue;

        /// <summary>
        /// A field naming a key in full, and under it what that key says in every locale. The read-only
        /// preview such a row otherwise carries is left off: the boxes below say the same thing and are
        /// the ones the author may correct.
        /// <para>The boxes follow the key as it is retyped. A dialogue's key is a box of its own, and
        /// boxes bound to the word it held a moment ago would show one key's text and write it under
        /// another.</para>
        /// </summary>
        private void AddKeyed(Node parent, FieldSchema field, JToken? value, string name, JsonPointer at,
            bool named, Control? actions)
        {
            var edit = new FieldEdit(this, _build, field, value, at, named);
            LineEdit box = Box(edit);

            // Always in a block: a key a field spells out in full is where a line of dialogue is worded,
            // and those run to sentences.
            var key = new TextKey([new LocalizedTextKey(string.Empty, edit.Written, _keyAnchor)], 0, block: true);

            box.TextChanged += key.Retype;

            // The next key this record lays down goes under this one, so a dialogue's lines arrive in the
            // file in the order they are read rather than one per end of file.
            if (edit.Written.Length > 0) _keyAnchor = edit.Written;

            parent.AddChild(Row(name, box, field.Documentation, actions));
            AddPair(Indented(parent), key);
        }

        /// <summary>The one way a key is written: a box per locale, in the order the files were read, the
        /// authoring one first. Both a record's own wording and a field naming a key are drawn through
        /// here, so there is one answer to what editing a translation looks like.</summary>
        private void AddPair(Node parent, TextKey key)
        {
            if (Texts is not { } texts) return;

            foreach (string locale in texts.Locales)
            {
                var text = new LocaleText(this, _build, key, locale);

                parent.AddChild(Row(locale, key.Block ? TextBlock(text) : TextLine(text), TextHint, actions: null));
            }
        }

        /// <summary>A name, on one line.</summary>
        private static Control TextLine(LocaleText text)
        {
            var box = new LineEdit { Text = text.Read(), SizeFlagsHorizontal = SizeFlags.ExpandFill };

            box.TextChanged += text.Write;
            box.FocusExited += text.Seal;
            text.Follow(() => box.Text = text.Read());

            return box;
        }

        /// <summary>A description or a line of dialogue, in a block: the game's own wording runs to
        /// sentences, and a sentence read through a slot the width of a name is one nobody proofreads.</summary>
        private static Control TextBlock(LocaleText text)
        {
            var box = new TextEdit
            {
                Text = text.Read(),
                CustomMinimumSize = new Vector2(0, TextBlockHeight),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                WrapMode = TextEdit.LineWrappingMode.Boundary
            };

            box.TextChanged += () => text.Write(box.Text);
            box.FocusExited += text.Seal;
            text.Follow(() => box.Text = text.Read());

            return box;
        }

        /// <summary>
        /// Whether the name just typed into the record's id is one the run refuses, and the run of
        /// keystrokes was therefore taken back. The catalogs are the judge of it, and the whole question
        /// is asked at once: a section is read over the whole folder into one table, so the second record
        /// of a name is the one the game drops, and a map some file already keys by the word would have
        /// one key swallow the other. Both are asked BEFORE anything is carried — a rename stopped
        /// halfway through the files is a state nobody asked for.
        /// <para>Asked only where the id actually changed, and only of the id: the run just sealed is
        /// what an undo takes back here, and a field nobody typed in has no run of its own to lose.</para>
        /// </summary>
        private bool Refused(JsonPointer at)
        {
            if (Renaming(at) is not { } rename) return false;
            if (Refusal(rename) is not { } refusal) return false;

            TakeBack(rename.Record);
            Say(refusal);

            return true;
        }

        /// <summary>Why the word will not do, or null when it will. Asked only of a record the catalog
        /// lists: a row standing inside one is named by nothing outside its own file, so no section lists
        /// it and no map anywhere is keyed by it — and the section's own table, asked about such a row,
        /// would refuse it the name of the very record hosting it.</summary>
        private string? Refusal(Rename rename) =>
            !rename.Listed ? null
                : Uses is { } uses
                    ? CatalogEditing.RenameRefusal(uses, rename.View, rename.Record, _idBefore, rename.Now)
                    : CatalogEditing.RenameRefusal(rename.View, rename.Record, rename.Now);

        /// <summary>The rename this seal is about, or nothing at all: the address has to be the record's
        /// own id, and the word standing in it has to have changed since the run of keystrokes began.</summary>
        private Rename? Renaming(JsonPointer at)
        {
            if (_record is not { } record || References?.Holding(record.File) is not { } view) return null;
            if (record.Schema.IdField is not { } id || at != record.Pointer.Append(id)) return null;

            string now = record.CurrentId;

            if (string.Equals(now, _idBefore, StringComparison.Ordinal)) return null;

            return new Rename(view, record, now, Lists(view, record));
        }

        /// <summary>Whether the catalog lists this record itself rather than something standing inside
        /// one. A rename carries the references of a RECORD: a node of a conversation or a stage of a
        /// quest is named by nothing outside its own file, and asking the catalogs to rewrite every field
        /// holding that word would rewrite words that mean something else entirely.</summary>
        private static bool Lists(CatalogView view, CatalogRecord record)
        {
            foreach (CatalogRecord listed in view.Records)
                if (ReferenceEquals(listed.File, record.File) && listed.Pointer == record.Pointer)
                    return true;

            return false;
        }

        /// <summary>Takes back the run of keystrokes just sealed, which is the newest step of the tool:
        /// a refused rename costs the author nothing to undo and leaves nothing on the stack. Guarded and
        /// redrawn the way every write of this panel is — a redraw from inside the box being left would
        /// tear it down under the hand that left it.
        /// <para>The run is on top because nothing can have been filed over it: keystrokes into the id
        /// merge into that one command and nothing else, the seal that asks this arrives with the box
        /// being left, and the gesture that renames the keys is opened only after the answer.</para></summary>
        private void TakeBack(CatalogRecord record)
        {
            using (Writing()) record.File.Document.History.Undo();

            RebuildLater();
        }

        /// <summary>
        /// Carries the record's new name everywhere the run holds the old one, once the run of keystrokes
        /// over its id is over: every field of every catalog pointing at it, and the localization keys it
        /// is worded under. Done at the end of the run and not as the letters arrive — every half-typed
        /// word would otherwise be a rename of its own, and the files would follow the author's
        /// hesitation.
        /// <para>Only the id does this. A record taken out leaves its keys standing — whether a key is
        /// still read is a question about every catalog at once, and it is asked by an audit over the
        /// whole data root, not by the panel showing one record.</para>
        /// <para>A row standing INSIDE a record carries its wording alone: nothing outside the file names
        /// a node of a conversation or a stage of a quest, and how such a row is worded at all is the
        /// host's answer rather than a suffix of the record's id.</para>
        /// <para>Keys ANOTHER catalog words from this id — the lines of a conversation keyed by the npc it
        /// belongs to — stand where they are: they are that catalog's own plan, run where it is edited.</para>
        /// <para>Answers whether anything moved. The boxes on screen are written under the keys the OLD id
        /// worded and have to be drawn again — but the drawing is one decision taken once the gesture is
        /// over, not one per thing that wrote.</para>
        /// </summary>
        private bool Carried(JsonPointer at)
        {
            if (Renaming(at) is not { } rename) return false;

            bool moved = Uses is { } uses && rename.Listed ? Everywhere(uses, rename) : Worded(rename);

            if (moved) _idBefore = rename.Now;

            return moved;
        }

        /// <summary>The whole rename: the references and the wording in one step of the history with the
        /// id the author typed, so a record stepped back to its old name is named under that name by
        /// everything again. What it came to is said out loud — an author renaming a record written in a
        /// dozen places has to see that they moved, and that the wording either followed or did not.</summary>
        private bool Everywhere(ReferenceUses uses, Rename rename)
        {
            CatalogRenameResult result;

            // Guarded: the new word is written into every file holding the old one, and one of them is
            // often the very document this panel follows — a record naming another of its own catalog, or
            // two sections of one file. Hearing about that from inside the box being left would tear the
            // box down under the hand that left it, and the panel is drawn once the gesture is over.
            using (Writing())
                result = CatalogEditing.RenameEverywhere(
                    uses, rename.View, rename.Record, _idBefore, rename.Now, Texts, _suffixes);

            // A refusal reaching this far means the run answered one way while the word was being judged
            // and another while it was being carried: said out loud rather than swallowed, and nothing on
            // screen is redrawn, because nothing was written.
            if (result.Refused is { } refusal)
            {
                Say(refusal);

                return false;
            }

            string done = Text(RenamedFormat, result.Uses, result.Files, result.Keys.Renamed);

            if (result.Keys.Taken is { } taken) Say(Text(KeysStayedFormat, done, _idBefore, taken));
            else if (Moved(result)) Say(done);

            return true;
        }

        /// <summary>Whether the rename came to anything at all. The run reads a word the panel calls a
        /// rename as none — a space typed onto the end of a name, the first name of a record that had none
        /// — and a line saying that nothing moved anywhere reads as a gesture that failed.</summary>
        private static bool Moved(CatalogRenameResult result) =>
            result.Uses + result.Files + result.Keys.Renamed > 0;

        /// <summary>The wording alone, for a row of a record the catalog does not list. One step of the
        /// history with the word the author typed, for the reason the whole rename is one: a row read
        /// under one word by the file and under another by every locale is what nobody can see.</summary>
        private bool Worded(Rename rename)
        {
            if (Texts is not { } texts) return false;

            JsonTreeDocument written = rename.Record.File.Document;

            using (written.History.GroupWithNewest(Text(RenameStepFormat, _idBefore, rename.Now), written))
            {
                LocalizedRename moved = texts.RenameRecord(_idBefore, rename.Now, _suffixes);

                if (moved.Taken is { } taken) Say(Text(KeysNotMovedFormat, _idBefore, taken));
                else if (moved.Renamed > 0) Say(Text(RenamedKeysFormat, moved.Renamed));
            }

            return true;
        }

        /// <summary>Lets the host word the record again now that the gesture is over, and answers whether
        /// it wrote anything. Guarded the way every write of this panel is: what the host writes goes into
        /// the very document the panel follows, and hearing about it from inside a box being left would
        /// tear that box down under the hand leaving it.</summary>
        private bool Settle()
        {
            if (Settled is not { } settled) return false;

            using (Writing()) return settled();
        }

        /// <summary>Holds the panel through a write of its own and gives back what it was holding when the
        /// write is over: the document tells everyone it changed, this panel included, and redrawing from
        /// inside the gesture doing the writing would tear down the control the author's hand is on.
        /// <para>Given back rather than lowered, so a write standing inside another leaves the outer one
        /// guarded to its own end.</para></summary>
        private WriteGuard Writing() => new(this);

        /// <summary>What a gesture came to when the record itself cannot show it, on the line that says
        /// what the run last did.</summary>
        private void Say(string what) => Said?.Invoke(what);

        // ── gestures ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A button that changes the shape of the record. Disabled where there is nothing for it to do,
        /// with the reason on it: a row whose element cannot move up says which row it is, and a button
        /// that answered nothing would read as the tool having missed the press.
        /// </summary>
        private Button Gesture(string text, string hint, bool enabled, Func<JsonTreeDocument, bool> change)
        {
            var button = new Button { Text = text, TooltipText = hint, Disabled = !enabled };
            object build = _build;

            button.Pressed += () =>
            {
                if (!Stale(build)) Restructure(change);
            };

            return button;
        }

        /// <summary>What may be done to one element where it stands. The ends of the list are shown as
        /// buttons that cannot be pressed rather than as buttons that are not there: the rows stay one
        /// column, and the first row says it is the first.</summary>
        private Control ElementGestures(JsonPointer list, int index, int count)
        {
            var row = new HBoxContainer();
            JsonPointer element = list.Append(index);

            row.AddChild(Gesture(UpText, index > 0 ? MoveUpHint : FirstElementHint, index > 0,
                document => document.Move(element, index - 1)));

            row.AddChild(Gesture(DownText, index < count - 1 ? MoveDownHint : LastElementHint, index < count - 1,
                document => document.Move(element, index + 1)));

            row.AddChild(Gesture(RemoveText, RemoveElementHint, enabled: true, document => document.Remove(element)));

            return row;
        }

        /// <summary>What may be done to one field of a record where it stands. A key the schema requires
        /// is not one of them: emptying it is what the field's own control is for, and a record without
        /// it is a record the game refuses to read.</summary>
        private Control FieldGesture(FieldSchema field, JToken? value, JsonPointer at)
        {
            if (field.Required) return new Button { Text = RemoveText, Disabled = true, TooltipText = RequiredFieldHint };
            if (value is null) return new Button { Text = RemoveText, Disabled = true, TooltipText = AbsentFieldHint };

            return Gesture(RemoveText, RemoveFieldHint, enabled: true, document => document.Remove(at));
        }

        /// <summary>The value a key of this panel is written with the moment the author asks for it. One
        /// place, so that the picker adding a key and the block drawing it never disagree about the shape
        /// the game reads there: a key answered by a vocabulary read as a list opens as a list.</summary>
        private JToken Blank(FieldSchema field) => TypedRecords.Blank(Vocabularies?.Invoke(field), field);

        /// <summary>The row that adds an element to a list, at the end of the list it adds to.</summary>
        private Control NewElement(FieldSchema item, JsonPointer at, int count)
        {
            Button add = Gesture(AddElementText, AddElementHint, enabled: true,
                document => document.Insert(at, count, Blank(item)));

            add.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            return add;
        }

        /// <summary>
        /// The row that adds a key to a map: the keys the schema names, offered as a list with the taken
        /// ones left out of it, or a box when the author writes the key himself.
        /// <para>A reference key is written rather than offered for the same reason a reference field is:
        /// what may answer it lives in a catalog this panel has not read, so it says which catalogs those
        /// are and leaves the word to the author.</para>
        /// </summary>
        private Control NewKey(FieldSchema field, FieldSchema item, JObject map, JsonPointer at)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            if (field.Key is { Kind: FieldKind.Enum } key) AddMemberKey(row, key, item, map, at);
            else AddWrittenKey(row, field.Key ?? item, item, map, at);

            return row;
        }

        private void AddMemberKey(Node row, FieldSchema key, FieldSchema item, JObject map, JsonPointer at)
        {
            List<string> free = [.. key.EnumValues.Where(member => !map.ContainsKey(member))];
            bool any = free.Count > 0;

            var picker = new OptionButton
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Disabled = !any,
                TooltipText = any ? AddKeyHint : EveryKeyHint
            };

            foreach (string member in free) picker.AddItem(member);

            picker.Selected = any ? 0 : NoChoice;

            row.AddChild(picker);
            row.AddChild(Gesture(AddKeyText, any ? AddKeyHint : EveryKeyHint, any,
                document => Added(document, map, item, at, Picked(picker, free))));
        }

        private void AddWrittenKey(Node row, FieldSchema key, FieldSchema item, JObject map, JsonPointer at)
        {
            string hint = Hint(key) is { Length: > 0 } written ? written : KeyPlaceholder;
            object build = _build;

            var box = new LineEdit
            {
                PlaceholderText = hint,
                TooltipText = hint,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            // Enter adds it too: the box and the button stand together, and a key typed into one and then
            // submitted to nothing reads as the map having refused the word.
            box.TextSubmitted += typed =>
            {
                if (!Stale(build)) Restructure(document => Added(document, map, item, at, typed));
            };

            row.AddChild(box);
            row.AddChild(Gesture(AddKeyText, AddKeyHint, enabled: true,
                document => Added(document, map, item, at, box.Text)));
        }

        /// <summary>The member a picker stands on, or nothing when it stands on none.</summary>
        private static string Picked(OptionButton picker, IReadOnlyList<string> members) =>
            picker.Selected >= 0 && picker.Selected < members.Count ? members[picker.Selected] : string.Empty;

        /// <summary>Writes a key into a map with the blank of what the map holds under it. A key nobody
        /// named and a key already there are said out loud: an add that answered nothing would read as
        /// the map having refused a word the author is looking straight at.</summary>
        private bool Added(JsonTreeDocument document, JObject map, FieldSchema item, JsonPointer at, string key)
        {
            string wanted = key.Trim();

            if (wanted.Length == 0)
            {
                Said?.Invoke(NoKeyText);
                return false;
            }

            if (map.ContainsKey(wanted))
            {
                Said?.Invoke(Text(KeyTakenFormat, wanted));
                return false;
            }

            return document.Insert(at, wanted, Blank(item));
        }

        /// <summary>The row that writes one of a record's keys into it, offering those the record is drawn
        /// by and the file does not hold — the shape's keys and the record's own alike, so that a key
        /// living outside the shape is one the author can still lay down. Null when the file holds all of
        /// them: a picker with nothing in it is a row that only takes up space.</summary>
        private Control? NewField(IReadOnlyList<FieldSchema> written, RecordSchema declared, JObject holder, JsonPointer at)
        {
            List<FieldSchema> absent = [.. written.Where(field =>
                !field.Hidden && !NamesTheForm(declared, field) && !holder.ContainsKey(field.JsonName))];

            if (absent.Count == 0) return null;

            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = AddFieldHint };

            foreach (FieldSchema field in absent) picker.AddItem(field.JsonName);

            picker.Selected = 0;

            row.AddChild(picker);
            row.AddChild(Gesture(AddFieldText, AddFieldHint, enabled: true, document =>
                picker.Selected >= 0
                && picker.Selected < absent.Count
                && document.Insert(at, absent[picker.Selected].JsonName, Blank(absent[picker.Selected]))));

            return row;
        }

        /// <summary>The shape of a polymorphic record, picked. Nothing is selected while the record wears
        /// a shape the schema does not list: the picker is what the author fixes it with, and standing on
        /// the first shape would be claiming the record already is one.</summary>
        private Control FormPicker(VariantSet variants, VariantSchema? worn, JsonPointer at)
        {
            var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = FormHint };
            object build = _build;

            for (int index = 0; index < variants.Variants.Count; index++)
                picker.AddItem(variants.Variants[index].DiscriminatorValue, index);

            picker.Selected = worn is null ? NoChoice : IndexOf(variants, worn);

            picker.ItemSelected += index =>
            {
                if (Stale(build)) return;

                VariantSchema chosen = variants.Variants[(int)index];

                if (!ReferenceEquals(chosen, worn))
                    Restructure(document => RecordTemplates.SwitchVariant(document, at, variants, chosen));
            };

            return picker;
        }

        private static int IndexOf(VariantSet variants, VariantSchema worn)
        {
            for (int index = 0; index < variants.Variants.Count; index++)
                if (ReferenceEquals(variants.Variants[index], worn))
                    return index;

            return NoChoice;
        }

        /// <summary>
        /// Changes the shape of the record and redraws once the gesture that asked for it is over.
        /// <para>Guarded the way a typed value is: the document tells everyone it changed, this panel
        /// included, and a redraw from inside the press would tear down the very button being pressed.
        /// The run of keystrokes the author was on is ended on both sides of it, so a structural step is
        /// never the tail of a word being typed and never swallows the next one.</para>
        /// </summary>
        private bool Restructure(Func<JsonTreeDocument, bool> change)
        {
            if (_document is not { } document) return false;

            _writing = true;
            bool changed;

            try
            {
                document.History.Seal();
                changed = change(document);

                // Before the seal, so that what the host writes about the wording is part of the gesture
                // the author made and not a step of its own standing after it. What it answers is not
                // asked here: the shape of the record changed, so the panel is drawn again either way.
                if (changed) _ = Settled?.Invoke();

                document.History.Seal();
            }
            finally
            {
                _writing = false;
            }

            if (changed) RebuildLater();

            return changed;
        }

        /// <summary>Whether a control still speaks for what is on screen.</summary>
        private bool Stale(object build) => !ReferenceEquals(build, _build);

        /// <summary>
        /// Puts a value at an address, adding the key when the file does not hold it yet — the first
        /// edit of an absent field is what writes it into the record.
        /// <para>False when nothing was written: the control has outlived its build, or the record it
        /// belonged to is no longer in the document. Either way it is speaking for nothing on screen,
        /// and the panel has already been redrawn without it.</para>
        /// </summary>
        private bool Write(JsonPointer at, JToken value)
        {
            if (_document is null) return false;

            _writing = true;

            try
            {
                return _document.Put(at, value);
            }
            finally
            {
                _writing = false;
            }
        }

        /// <summary>Takes a key out of its record. False when there was none there to take.</summary>
        private bool Erase(JsonPointer at)
        {
            if (_document is null || _document.Resolve(at) is null) return false;

            _writing = true;

            try
            {
                return _document.Remove(at);
            }
            finally
            {
                _writing = false;
            }
        }

        /// <summary>A record whose id has just been retyped: the catalog holding its file, the record
        /// itself, the word now standing in the box, and whether the catalog lists the record or something
        /// inside one. Worked out again for each of the two questions the seal asks — whether the word may
        /// stand, and what carrying it comes to — so that neither is asked in the other's terms.</summary>
        private sealed record Rename(CatalogView View, CatalogRecord Record, string Now, bool Listed);

        /// <summary>The panel held through a write of its own, for as long as the write lasts, and put
        /// back the way it was found.</summary>
        private readonly struct WriteGuard : IDisposable
        {
            private readonly InspectorPanel _panel;

            private readonly bool _held;

            public WriteGuard(InspectorPanel panel)
            {
                _panel = panel;
                _held = panel._writing;
                panel._writing = true;
            }

            public void Dispose() => _panel._writing = _held;
        }

        /// <summary>One field of the record as a control writes it: what the schema says about it, what
        /// stands there now, where it goes, and which build of the panel it belongs to.</summary>
        private sealed class FieldEdit(InspectorPanel panel, object build, FieldSchema schema, JToken? value,
            JsonPointer at, bool named)
        {
            public FieldSchema Field => schema;

            public JToken? Value => value;

            /// <summary>Whether leaving this value out means leaving out a key. An element of a list and a
            /// value of a map go with the element itself, which is a gesture of its own.</summary>
            public bool Removable => named;

            /// <summary>What the file holds, spelled the way the file spells it; empty for a key it does
            /// not hold at all.</summary>
            public string Written => value is null || value.Type == JTokenType.Null
                ? string.Empty
                : JsonScalars.Written(value);

            /// <summary>Whether the run can answer this field at all: it points somewhere, and the ids
            /// of what it points into have been read.</summary>
            public bool Offered => panel.References is not null && schema.RefTargets.Count > 0;

            /// <summary>The ids the run knows, for the judgement on the word standing in this field.</summary>
            public ReferenceIndex? References => panel.References;

            public bool Write(JToken written) => !panel.Stale(build) && panel.Write(at, written);

            public bool Erase() => !panel.Stale(build) && panel.Erase(at);

            /// <summary>The text a key stands for, asked of the run rather than of the engine.</summary>
            public string Translation(string key) => panel.Translation(key);

            /// <summary>The ids of those targets a query names, best first.</summary>
            public IReadOnlyList<string> Search(string query) =>
                panel.References?.Search(schema.RefTargets, query, ReferencePicker.Rows) ?? [];

            /// <summary>Offers those ids under <paramref name="under"/>, and hands back the one picked.</summary>
            public void Pick(Control under, Action<string> chosen)
            {
                if (!panel.Stale(build)) ReferencePicker.Open(panel, under, Search, chosen);
            }

            /// <summary>Ends the run of keystrokes this field was taking, so the next field edited is a
            /// step of its own — and, where the run was over the record's id, carries that name everywhere
            /// the run writes it and names the localization keys worded from it again.
            /// <para>Both the record's own rename and the host's wording are asked, and the panel is drawn
            /// once for whichever of them wrote: the boxes are written under keys that may have moved, and
            /// drawing them twice is a panel the author cannot keep his place in. Not drawn at all where nothing
            /// moved — a gesture ends every time a box is left, and most of them leave the wording where
            /// it was.</para></summary>
            public void Seal()
            {
                panel._document?.History.Seal();

                // Not from a control that has outlived its build: it speaks for a record no longer on
                // screen, and the word the keys would be named from is another record's.
                if (panel.Stale(build)) return;

                // A name the run refuses — one the section already writes, one a map is already keyed by —
                // is taken back where it was typed, and nothing else moves: the record is called what it
                // was called a moment ago.
                if (panel.Refused(at)) return;

                bool renamed = panel.Carried(at);
                bool worded = panel.Settle();

                if (renamed || worded) Redraw();
            }

            public void Redraw() => panel.RebuildLater();
        }

        /// <summary>The key a pair of boxes is written under, and the news that it has been retyped. A
        /// record's own key stands still; a key a field spells out changes under the boxes as the author
        /// types it, and they have to follow or they write his words under a key nothing reads.</summary>
        private sealed class TextKey(List<LocalizedTextKey> family, int index, bool block)
        {
            public event Action? Retyped;

            /// <summary>The record's keys in the order its catalog words them, shared by every pair of
            /// boxes of that record: where a key is laid down depends on which of its neighbours are
            /// written already, and that is a question about the family and not about one key.</summary>
            public IReadOnlyList<LocalizedTextKey> Family => family;

            public int Index => index;

            public LocalizedTextKey Key => family[index];

            /// <summary>Whether the text is written in a block rather than on a line.</summary>
            public bool Block => block;

            public void Retype(string written)
            {
                if (string.Equals(written, Key.Key, StringComparison.Ordinal)) return;

                family[index] = Key with { Key = written };
                Retyped?.Invoke();
            }
        }

        /// <summary>
        /// One locale's text under one key: what a box shows, and what it writes back. Held apart from
        /// the control because a line and a block share no base that carries their text — this is what
        /// makes them two ways of showing one behaviour rather than two behaviours.
        /// </summary>
        private sealed class LocaleText(InspectorPanel panel, object build, TextKey key, string locale)
        {
            public string Read() => panel.Texts?.Read(locale, key.Key.Key) ?? string.Empty;

            /// <summary>Writes the text as it is typed. The key is laid down in every locale by the first
            /// letter of it; a refusal is said out loud, because a box that swallows what is typed into
            /// it reads as the tool having lost the words.</summary>
            public void Write(string text)
            {
                if (panel.Stale(build) || panel.Texts is not { } texts) return;

                string at = key.Key.Key;

                // Nothing to say about a field naming no key yet: the author is still typing it into the
                // row above, and that is where the question is answered.
                if (at.Length == 0) return;

                if (texts.Write(locale, key.Family, key.Index, text) || text.Length == 0) return;

                panel.Say(Text(TextRefusedFormat, at, locale));
            }

            /// <summary>Ends the run of keystrokes this box was taking, so the next text written is a step
            /// of its own.</summary>
            public void Seal() => panel.Texts?.Seal();

            /// <summary>Shows the text again whenever the key the box is written under changes.</summary>
            public void Follow(Action show) => key.Retyped += show;
        }
    }
}
