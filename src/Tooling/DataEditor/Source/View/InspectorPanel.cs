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
    /// <para>Only scalars a record names are edited here. An element of a list and a value of a free map
    /// are read: what they need is a way to add, remove and reorder, which is a gesture the panel does
    /// not have yet, and half of it — a value editable where its neighbour cannot be removed — reads as
    /// the other half being broken.</para>
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
        private const string CatalogSeparator = ", ";

        /// <summary>How "no value at all" is offered when the game reads something in its place: the
        /// author choosing it has to be able to see what leaving the key out actually means.</summary>
        private const string MissingDefaultFormat = "{0}   ({1})";

        private const string FontSizeOverride = "font_size";
        private const string MarginLeftOverride = "margin_left";

        /// <summary>Which control answers for which kind of value — the one place a kind and a widget
        /// are put together. A kind absent from here has no editor and is drawn as text, which is what
        /// keeps objects, lists, maps and free json read-only without a second rule saying so.</summary>
        private static readonly Dictionary<FieldKind, Func<FieldEdit, Control>> s_editors = new()
        {
            [FieldKind.String] = TextBox,
            [FieldKind.Reference] = TextBox,
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
        /// it already is.</summary>
        private static Node Section(Node parent, string name)
        {
            parent.AddChild(Header(name));

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride(MarginLeftOverride, Indent);

            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            margin.AddChild(body);
            parent.AddChild(margin);

            return body;
        }

        /// <summary>One field as a row. What the field means hangs off its name as a tooltip: the panel
        /// is read down the value column, and a sentence per row printed in full would push the values
        /// apart until the record no longer reads as one thing.</summary>
        private static Control Row(string name, Control value, string? documentation)
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

            return row;
        }

        private static Label ValueLabel(string text) => new()
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        /// <summary>The shape a polymorphic record is actually written in. A record whose shape the
        /// schema does not list is said out loud and then drawn by the shape it was declared with: the
        /// author has to see that the file names something the game will not recognise.</summary>
        private static RecordSchema Shape(Node parent, RecordSchema schema, JToken token)
        {
            if (schema.Variants is not { } variants) return schema;

            foreach (VariantSchema variant in variants.Variants)
                if (Wears(token, variants.Discriminator, variant.DiscriminatorValue))
                    return variant.Record;

            parent.AddChild(new Label { Text = UnknownShapeText });

            return schema;
        }

        /// <summary>Whether a record is of one shape: by the value of the field that names shapes, or,
        /// with no such field, by the presence of the key the shape is known by.</summary>
        private static bool Wears(JToken token, string? discriminator, string value)
        {
            if (token is not JObject holder) return false;

            return discriminator is null
                ? holder.ContainsKey(value)
                : string.Equals(holder[discriminator]?.ToString(), value, StringComparison.Ordinal);
        }

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
                FieldKind.Reference when field.RefCatalogs.Count > 0 =>
                    Text(ReferenceFormat, written, string.Join(CatalogSeparator, field.RefCatalogs)),
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

        /// <summary>What an empty box says instead of standing blank: the catalogs a reference may name,
        /// or the value the game reads while the key is absent.</summary>
        private static string Hint(FieldSchema field)
        {
            if (field.Kind == FieldKind.Reference && field.RefCatalogs.Count > 0)
                return string.Join(CatalogSeparator, field.RefCatalogs);

            return DefaultText(field.Default);
        }

        private static Control TextBox(FieldEdit edit) => Box(edit);

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
        /// a blank box with no way to learn what the game does with it.</para>
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

            if (!edit.Field.Required || written.Length == 0) picker.AddItem(Absent(edit.Field), none);

            for (int index = 0; index < members.Count; index++) picker.AddItem(members[index], index);

            int at = members.IndexOf(written);

            picker.Selected = picker.GetItemIndex(at >= 0 ? at : none);

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
            RecordSchema written = Shape(parent, schema, token);

            foreach (FieldSchema field in written.Fields)
            {
                if (field.Hidden) continue;

                AddField(parent, field, Value(token, field.JsonName), field.JsonName, at.Append(field.JsonName),
                    named: true, depth);
            }
        }

        /// <summary>
        /// One field, drawn by what it holds. <paramref name="named"/> is what tells a field of a record
        /// from an element of a list or an entry of a free map: only the first has a key of its own to
        /// write under, and only the first is edited here.
        /// </summary>
        private void AddField(Node parent, FieldSchema field, JToken? value, string name, JsonPointer at,
            bool named, int depth)
        {
            if (depth >= MaxDepth || value is null || value.Type == JTokenType.Null)
            {
                parent.AddChild(Row(name, Editor(field, value, at, named), field.Documentation));
                return;
            }

            switch (field.Kind)
            {
                case FieldKind.Object when field.Record is { } record && value is JObject:
                    AddRecord(Section(parent, name), record, value, at, depth + 1);
                    break;

                case FieldKind.Array when field.Item is { } item && value is JArray array:
                    Node elements = Section(parent, Text(CountFormat, name, array.Count));

                    for (int index = 0; index < array.Count; index++)
                        AddField(elements, item, array[index], Text(IndexFormat, index), at.Append(index),
                            named: false, depth + 1);

                    break;

                case FieldKind.Dictionary when field.Item is { } item && value is JObject map:
                    Node pairs = Section(parent, Text(CountFormat, name, map.Count));

                    foreach (JProperty pair in map.Properties())
                        AddField(pairs, item, pair.Value, pair.Name, at.Append(pair.Name), named: false, depth + 1);

                    break;

                default:
                    parent.AddChild(Row(name, Editor(field, value, at, named), field.Documentation));
                    break;
            }
        }

        /// <summary>The control a field is changed through, or the text it is read as. A value the panel
        /// will not take responsibility for writing is read: a field of a list, a kind no control
        /// answers for, and a structure standing where the schema expected a value — a box over that
        /// last one would write a word across whatever the author actually wrote there.</summary>
        private Control Editor(FieldSchema field, JToken? value, JsonPointer at, bool named)
        {
            if (!named || _document is null || value is JObject or JArray)
                return ValueLabel(Scalar(field, value));

            return s_editors.TryGetValue(field.Kind, out Func<FieldEdit, Control>? build)
                ? build(new FieldEdit(this, _build, field, value, at))
                : ValueLabel(Scalar(field, value));
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
        private sealed class FieldEdit(InspectorPanel panel, object build, FieldSchema schema, JToken? value, JsonPointer at)
        {
            public FieldSchema Field => schema;

            public JToken? Value => value;

            /// <summary>What the file holds, spelled the way the file spells it; empty for a key it does
            /// not hold at all.</summary>
            public string Written => value is null || value.Type == JTokenType.Null
                ? string.Empty
                : JsonScalars.Written(value);

            public bool Write(JToken written) => !panel.Stale(build) && panel.Write(at, written);

            public bool Erase() => !panel.Stale(build) && panel.Erase(at);

            /// <summary>Ends the run of keystrokes this field was taking, so the next field edited is a
            /// step of its own.</summary>
            public void Seal() => panel._document?.History.Seal();

            public void Redraw() => panel.RebuildLater();
        }
    }
}
