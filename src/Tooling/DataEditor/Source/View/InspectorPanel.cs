namespace DataEditor.Source.View
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
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

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
        private const string TargetSeparator = ", ";

        /// <summary>How "no value at all" is offered when the game reads something in its place: the
        /// author choosing it has to be able to see what leaving the key out actually means.</summary>
        private const string MissingDefaultFormat = "{0}   ({1})";

        /// <summary>The name the shape of a polymorphic record is picked under.</summary>
        private const string FormName = "form";

        private const string AddElementText = "+ element";
        private const string AddKeyText = "+ key";
        private const string AddFieldText = "+ field";
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

        /// <summary>What an empty key box says while the schema has nothing to say about the keys.</summary>
        private const string KeyPlaceholder = "key";

        private const string KeyTakenFormat = "“{0}” is already there";
        private const string NoKeyText = "name the key to add";

        private const string FontSizeOverride = "font_size";
        private const string FontColorOverride = "font_color";
        private const string MarginLeftOverride = "margin_left";

        /// <summary>How wide and how tall the list of ids a reference is picked from opens.</summary>
        private const int PickerWidth = 340;
        private const int PickerHeight = 300;

        /// <summary>How many ids the picker offers at once. A query of one letter answers with most of a
        /// catalog, and a list nobody scrolls to the end of is a list that only costs rows.</summary>
        private const int PickerRows = 60;

        private const string PickText = "…";
        private const string PickHint = "pick an id out of what this field points into";
        private const string SearchPlaceholder = "search";

        private const string BrokenReferenceFormat = "nothing in {0} is written under this id";
        private const string EmptyReferenceText = "this field has to name something";
        private const string UncheckedReferenceFormat = "{0}: this build describes no such catalog or section, so the id is not checked";

        /// <summary>What a word answering to nothing is written in. A reference is read down a column of
        /// them, and the one that points nowhere has to be the one the eye stops on.</summary>
        private static readonly Color BrokenReference = new(0.93f, 0.42f, 0.38f);

        /// <summary>Which control answers for which kind of value — the one place a kind and a widget
        /// are put together. A kind absent from here has no editor and is drawn as text, which is what
        /// keeps objects, lists, maps and free json read-only without a second rule saying so.</summary>
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

        /// <summary>Draws the record, or says why there is nothing to draw.</summary>
        public void Rebuild(CatalogRecord? record)
        {
            _record = record;

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
        private static Node Section(Node parent, string name, Control? actions)
        {
            parent.AddChild(actions is null ? Header(name) : Headed(name, actions));

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride(MarginLeftOverride, Indent);

            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            margin.AddChild(body);
            parent.AddChild(margin);

            return body;
        }

        /// <summary>A heading with what may be done to what it names beside it.</summary>
        private static Control Headed(string name, Control actions)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            Label header = Header(name);

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

            // A label ignores the mouse, and a tooltip is only ever shown for a control the mouse can
            // land on; a row with nothing to say goes on ignoring it.
            if (documentation is { Length: > 0 })
            {
                label.TooltipText = documentation;
                label.MouseFilter = MouseFilterEnum.Pass;
            }

            row.AddChild(label);
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
        private static string Scalar(FieldSchema field, JToken? value)
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
                    Text(ReferenceFormat, written, string.Join(TargetSeparator, field.RefTargets)),
                FieldKind.LocalizedKey => Translated(written),
                _ => written
            };
        }

        /// <summary>A localization key together with the text it stands for. A key nothing translates
        /// answers with itself, and the panel says it once: the author is reading whether the key has
        /// been written yet, and a line repeating it says that as clearly as a line saying nothing.</summary>
        private static string Translated(string key)
        {
            string translation = Translation(key);

            return translation.Length == 0 ? key : Text(TranslationFormat, key, translation);
        }

        /// <summary>The text a key stands for, or nothing when it stands for itself — a key nobody has
        /// written a translation for yet.</summary>
        private static string Translation(string key)
        {
            if (key.Length == 0) return string.Empty;

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
                return string.Join(TargetSeparator, field.RefTargets);

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
            var pick = new Button { Text = PickText, TooltipText = PickHint, Disabled = !edit.Offered };

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

        /// <summary>
        /// Marks the word standing in a reference, and says on it what is wrong. Nothing is marked while
        /// the run cannot tell: a catalog this build describes no schema for writes ids the tool has
        /// never read, and painting every reference into it red would be the tool complaining about
        /// itself.
        /// </summary>
        private static void Judged(FieldEdit edit, LineEdit box, string written)
        {
            box.RemoveThemeColorOverride(FontColorOverride);

            // A field that names no catalog at all keeps the box it always was, hint and all: there is
            // nothing to check it against and nothing to offer instead.
            if (!edit.Offered) return;

            string named = string.Join(TargetSeparator, edit.Field.RefTargets);
            IReadOnlyList<string> unread = edit.Undescribed();

            if (unread.Count > 0)
            {
                box.TooltipText = Text(UncheckedReferenceFormat, string.Join(TargetSeparator, unread));
                return;
            }

            if (written.Length == 0 && edit.Field.AllowEmpty)
            {
                box.TooltipText = named;
                return;
            }

            if (written.Length > 0 && edit.Exists(written))
            {
                box.TooltipText = named;
                return;
            }

            box.AddThemeColorOverride(FontColorOverride, BrokenReference);
            box.TooltipText = written.Length == 0 ? EmptyReferenceText : Text(BrokenReferenceFormat, named);
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
                string text = Translation(key);

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
        private void Follow(JsonTreeDocument? document)
        {
            if (ReferenceEquals(document, _document)) return;

            _document?.Changed -= OnDocumentChanged;
            _document = document;
            _document?.Changed += OnDocumentChanged;
        }

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

            DropChildren();

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

            int drawn = GetChildCount();
            AddRecord(this, record.Schema, token, record.Pointer, depth: 0);

            // A record whose every field is machinery, or none at all: the panel says so rather than
            // standing empty under a heading, which reads as the tool having failed to draw it.
            if (GetChildCount() == drawn) AddChild(new Label { Text = NoFieldsText });
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

        /// <summary>
        /// Drops the rows of the previous record. <see cref="Node.QueueFree"/> on its own is not
        /// enough — it takes effect at the end of the frame, while the rebuild adds the replacements
        /// right away, so the panel would spend a frame showing both records.
        /// </summary>
        private void DropChildren()
        {
            foreach (Node child in GetChildren())
            {
                RemoveChild(child);
                child.QueueFree();
            }
        }

        private void AddRecord(Node parent, RecordSchema schema, JToken token, JsonPointer at, int depth)
        {
            RecordSchema written = Wearing(parent, schema, token, at);

            foreach (FieldSchema field in written.Fields)
            {
                if (field.Hidden || NamesTheForm(schema, field)) continue;

                JsonPointer key = at.Append(field.JsonName);
                JToken? value = Value(token, field.JsonName);

                AddField(parent, field, value, field.JsonName, key, named: true, depth, FieldGesture(field, value, key));
            }

            if (token is JObject holder && NewField(written, schema, holder, at) is { } add) parent.AddChild(add);
        }

        /// <summary>
        /// The shape a polymorphic record is written in, together with the picker that changes it. A
        /// record whose shape the schema does not list is said out loud and then drawn by the shape it
        /// was declared with: the author has to see that the file names something the game will not
        /// recognise, and the picker is how he answers it.
        /// </summary>
        private RecordSchema Wearing(Node parent, RecordSchema schema, JToken token, JsonPointer at)
        {
            if (schema.Variants is not { } variants) return schema;

            VariantSchema? worn = RecordTemplates.Worn(variants, token);

            if (_document is not null && token is JObject)
                parent.AddChild(Row(FormName, FormPicker(variants, worn, at), FormHint, actions: null));

            if (worn is null) parent.AddChild(new Label { Text = UnknownShapeText });

            return worn?.Record ?? schema;
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

        /// <summary>The row that adds an element to a list, at the end of the list it adds to.</summary>
        private Control NewElement(FieldSchema item, JsonPointer at, int count)
        {
            Button add = Gesture(AddElementText, AddElementHint, enabled: true,
                document => document.Insert(at, count, RecordTemplates.Blank(item)));

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

            return document.Insert(at, wanted, RecordTemplates.Blank(item));
        }

        /// <summary>The row that writes one of the record's own keys into it, offering the keys the schema
        /// names and the file does not hold. Null when it holds all of them — a picker with nothing in it
        /// is a row that only takes up space.</summary>
        private Control? NewField(RecordSchema written, RecordSchema declared, JObject holder, JsonPointer at)
        {
            List<FieldSchema> absent = [.. written.Fields.Where(field =>
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
                && document.Insert(at, absent[picker.Selected].JsonName, RecordTemplates.Blank(absent[picker.Selected]))));

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
                document.History.Seal();
            }
            finally
            {
                _writing = false;
            }

            if (changed) RebuildLater();

            return changed;
        }

        /// <summary>
        /// The ids of the catalogs a field points into, offered under the box it is written in: a query
        /// and the ids answering it, best match first. One click writes the id, which is what the author
        /// came for — a picker asking for a second confirming press is a list read twice.
        /// <para>Built for the press and freed when it closes. The panel is torn down and drawn again on
        /// every change, and a list held between two of those would be offering the ids of a record that
        /// is no longer on screen.</para>
        /// </summary>
        private void OpenPicker(FieldEdit edit, Control under, Action<string> chosen)
        {
            var popup = new PopupPanel();
            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
            var query = new LineEdit { PlaceholderText = SearchPlaceholder, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var results = new ItemList { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };

            void Fill(string typed)
            {
                results.Clear();

                foreach (string id in edit.Search(typed)) results.AddItem(id);
            }

            void Take(int index)
            {
                if (index < 0 || index >= results.ItemCount) return;

                chosen(results.GetItemText(index));
                popup.Hide();
            }

            Fill(string.Empty);

            query.TextChanged += Fill;

            // Enter takes the best hit: typing the id of a record and pressing return is the fast path
            // the search field exists for.
            query.TextSubmitted += _ => Take(0);
            results.ItemSelected += index => Take((int)index);

            body.AddChild(query);
            body.AddChild(results);
            popup.AddChild(body);
            popup.PopupHide += popup.QueueFree;

            AddChild(popup);

            var at = (Vector2I)(under.GetScreenPosition() + new Vector2(0, under.Size.Y));

            popup.Popup(new Rect2I(at, new Vector2I(Math.Max(PickerWidth, (int)under.Size.X), PickerHeight)));

            // Taken once the window is on screen, the way every other dialog of the tool takes it: a
            // window still being opened has no focus to hand out, and a search field that has to be
            // clicked before it can be typed into is a list the author scrolls instead.
            Callable.From(query.GrabFocus).CallDeferred();
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
                return _document.Resolve(at) is null && at.Parent is { } holder && at.Last is { } key
                    ? _document.Insert(holder, key, value)
                    : _document.SetValue(at, value);
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

            public bool Write(JToken written) => !panel.Stale(build) && panel.Write(at, written);

            public bool Erase() => !panel.Stale(build) && panel.Erase(at);

            /// <summary>Whether one of the targets this field names writes a record under that id.</summary>
            public bool Exists(string id) => panel.References?.Exists(schema.RefTargets, id) ?? false;

            /// <summary>The targets this field names that the run cannot read — an undescribed catalog, or a
            /// section no described catalog holds — and so cannot say anything about.</summary>
            public IReadOnlyList<string> Undescribed() => panel.References?.Undescribed(schema.RefTargets) ?? [];

            /// <summary>The ids of those targets a query names, best first.</summary>
            public IReadOnlyList<string> Search(string query) =>
                panel.References?.Search(schema.RefTargets, query, PickerRows) ?? [];

            /// <summary>Offers those ids under <paramref name="under"/>, and hands back the one picked.</summary>
            public void Pick(Control under, Action<string> chosen)
            {
                if (!panel.Stale(build)) panel.OpenPicker(this, under, chosen);
            }

            /// <summary>Ends the run of keystrokes this field was taking, so the next field edited is a
            /// step of its own.</summary>
            public void Seal() => panel._document?.History.Seal();

            public void Redraw() => panel.RebuildLater();
        }
    }
}
