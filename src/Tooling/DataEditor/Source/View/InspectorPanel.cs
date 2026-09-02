namespace DataEditor.Source.View
{
    using System;
    using Godot;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>
    /// Reads one record out to the author, field by field, in the order its schema names them. Nothing
    /// here edits: the panel exists to prove the schema and the document meet, and a control that took
    /// input would have to answer where the value goes.
    /// <para>The panel is rebuilt whole for every record. A record is small, and rebuilding is the one
    /// way of showing what the document holds now that cannot leave a stale row behind.</para>
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
        private const string IndexFormat = "[{0}]";
        private const string CatalogSeparator = ", ";

        private const string FontSizeOverride = "font_size";
        private const string MarginLeftOverride = "margin_left";

        /// <summary>Draws the record, or says why there is nothing to draw.</summary>
        public void Rebuild(CatalogRecord? record)
        {
            DropChildren();

            if (record is null)
            {
                AddChild(new Label { Text = EmptyText });
                return;
            }

            if (record.Token is not { } token)
            {
                AddChild(new Label { Text = GoneText });
                return;
            }

            AddChild(Header(Text(HeaderFormat, record.Id, record.Schema.TypeName)));

            int drawn = GetChildCount();
            AddRecord(this, record.Schema, token, depth: 0);

            // A record whose every field is machinery, or none at all: the panel says so rather than
            // standing empty under a heading, which reads as the tool having failed to draw it.
            if (GetChildCount() == drawn) AddChild(new Label { Text = NoFieldsText });
        }

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
        private static Control Row(string name, string value, string? documentation)
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

            row.AddChild(new Label
            {
                Text = value,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            });

            return row;
        }

        private static void AddRecord(Node parent, RecordSchema schema, JToken token, int depth)
        {
            RecordSchema written = Shape(parent, schema, token);

            foreach (FieldSchema field in written.Fields)
            {
                if (field.Hidden) continue;

                AddField(parent, field, Value(token, field.JsonName), field.JsonName, depth);
            }
        }

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

        private static void AddField(Node parent, FieldSchema field, JToken? value, string name, int depth)
        {
            if (depth >= MaxDepth || value is null || value.Type == JTokenType.Null)
            {
                parent.AddChild(Row(name, Scalar(field, value), field.Documentation));
                return;
            }

            switch (field.Kind)
            {
                case FieldKind.Object when field.Record is { } record && value is JObject:
                    AddRecord(Section(parent, name), record, value, depth + 1);
                    break;

                case FieldKind.Array when field.Item is { } item && value is JArray array:
                    Node elements = Section(parent, Text(CountFormat, name, array.Count));

                    for (int index = 0; index < array.Count; index++)
                        AddField(elements, item, array[index], Text(IndexFormat, index), depth + 1);

                    break;

                case FieldKind.Dictionary when field.Item is { } item && value is JObject map:
                    Node pairs = Section(parent, Text(CountFormat, name, map.Count));

                    foreach (JProperty pair in map.Properties())
                        AddField(pairs, item, pair.Value, pair.Name, depth + 1);

                    break;

                default:
                    parent.AddChild(Row(name, Scalar(field, value), field.Documentation));
                    break;
            }
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
            string translation = TranslationServer.Translate(key).ToString();

            return string.Equals(translation, key, StringComparison.Ordinal) ? key : Text(TranslationFormat, key, translation);
        }

        private static string Raw(JToken token)
        {
            string json = token.ToString(Formatting.None);

            return json.Length <= RawLimit ? json : json[..RawLimit] + Ellipsis;
        }

        private static JToken? Value(JToken token, string name) =>
            token is JObject holder && holder.TryGetValue(name, StringComparison.Ordinal, out JToken? value) ? value : null;
    }
}
