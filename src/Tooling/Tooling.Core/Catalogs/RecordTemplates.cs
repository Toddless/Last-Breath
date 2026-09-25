namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// What a value looks like the moment the author asks for one — a fresh element of a list, a fresh
    /// record, the keys a shape brings with it — and what changing the shape of a polymorphic record does
    /// to the keys it already has.
    /// <para>Kept away from the panel offering those gestures: a blank the author cannot correct is a
    /// value written into a file for nothing, and the rule deciding it has to be readable without a
    /// window in front of it.</para>
    /// </summary>
    public static class RecordTemplates
    {
        /// <summary>Which empty value answers for which kind — the one place a kind and its blank are put
        /// together, the way the inspector puts a kind and its control together. A kind absent from here
        /// is written as an empty object, which is the shape that carries anything at all.</summary>
        private static readonly Dictionary<FieldKind, Func<FieldSchema, JToken>> s_blanks = new()
        {
            [FieldKind.String] = _ => new JValue(string.Empty),
            [FieldKind.Reference] = _ => new JValue(string.Empty),
            [FieldKind.LocalizedKey] = _ => new JValue(string.Empty),
            [FieldKind.Integer] = _ => new JValue(0L),
            [FieldKind.Number] = _ => new JValue(0d),
            [FieldKind.Boolean] = _ => new JValue(false),
            [FieldKind.Enum] = Member,
            [FieldKind.Object] = Nested,
            [FieldKind.Array] = _ => new JArray(),
            [FieldKind.Dictionary] = _ => new JObject(),
            [FieldKind.Any] = _ => new JObject()
        };

        /// <summary>
        /// The value a field is written with when the author asks for it and has said nothing about it
        /// yet: the default the schema names, or the empty value of its kind.
        /// <para>The default is preferred because writing the key out with it changes nothing the game
        /// does — the author has made a key visible, not changed what the record means.</para>
        /// </summary>
        public static JToken Blank(FieldSchema field)
        {
            ArgumentNullException.ThrowIfNull(field);

            JToken blank = s_blanks.TryGetValue(field.Kind, out Func<FieldSchema, JToken>? build)
                ? build(field)
                : new JObject();

            // Only a value has a default to take: a list or a map the schema gave one would be describing
            // a structure in a word, and the empty structure is the honest reading of it.
            return blank is JValue && Chosen(field.Default) is { } chosen ? chosen : blank;
        }

        /// <summary>
        /// A record with the keys it cannot be written without and no others. What the schema leaves
        /// optional is left out on purpose: the author adds it when he means it, and a record laid down
        /// with every key of its type would say things about itself that nobody decided.
        /// <para>A record taking several shapes is required to hold its OWN keys as much as the shape's:
        /// what stays on the record — the inversion every predicate shares, the tier every augment has —
        /// is asked of it whichever shape it wears. An id among them arrives empty like any other required
        /// key: WHICH word a record is listed under is its catalog's answer, written onto the blank by
        /// whoever adds the record to a section.</para>
        /// </summary>
        public static JObject Blank(RecordSchema record)
        {
            ArgumentNullException.ThrowIfNull(record);

            // A record with shapes is written in one of them, and the first is the one the schema lists
            // first: a record wearing no shape at all is not a record the game can read.
            return record.Variants is { } variants
                ? Filled(Wear(new JObject(), variants, variants.Variants[0]), record, variants.Discriminator)
                : Filled(new JObject(), record, skip: null);
        }

        /// <summary>The shape a record is actually written in, or null when it is written in none the
        /// schema lists. Told by the value of the field that names shapes or, with no such field, by the
        /// presence of the key the shape is known by.</summary>
        public static VariantSchema? Worn(VariantSet variants, JToken? token)
        {
            ArgumentNullException.ThrowIfNull(variants);

            if (token is not JObject holder) return null;

            foreach (VariantSchema variant in variants.Variants)
                if (Wears(holder, variants.Discriminator, variant.DiscriminatorValue))
                    return variant;

            return null;
        }

        /// <summary>
        /// Writes the record at <paramref name="record"/> in another of its shapes: the keys only the
        /// outgoing shape names go, the keys the incoming one cannot be without arrive with their blanks,
        /// and everything both name — and everything neither does — stays exactly as the file has it.
        /// <para>The record is replaced whole, so the change is ONE step of the history: a shape half
        /// taken back is a record naming both of two things it must name one of, which is a state no
        /// author asked for and the file cannot be read in.</para>
        /// <para>False when the document has no record there, or when it is already written in that
        /// shape.</para>
        /// </summary>
        public static bool SwitchVariant(JsonTreeDocument document, JsonPointer record, VariantSet variants, VariantSchema form)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(variants);
            ArgumentNullException.ThrowIfNull(form);

            if (document.Resolve(record) is not JObject holder) return false;
            if (ReferenceEquals(Worn(variants, holder), form)) return false;

            return document.SetValue(record, Wear((JObject)holder.DeepClone(), variants, form));
        }

        private static bool Wears(JObject holder, string? discriminator, string value) =>
            discriminator is null
                ? holder.ContainsKey(value)
                : string.Equals(holder[discriminator]?.ToString(), value, StringComparison.Ordinal);

        /// <summary>Dresses an object in one shape: sheds what only the shape it wore names, fills in what
        /// the new one requires, and writes the word that says which shape this is.</summary>
        private static JObject Wear(JObject holder, VariantSet variants, VariantSchema form)
        {
            foreach (string key in Shed(Worn(variants, holder), form)) holder.Remove(key);

            Filled(holder, form.Record, variants.Discriminator);

            if (variants.Discriminator is { } named)
            {
                holder[named] = new JValue(form.DiscriminatorValue);

                return holder;
            }

            // With no field naming the shapes, the presence of the key IS the shape: a record that does not
            // carry it is written in no shape at all, whatever else it holds.
            if (!holder.ContainsKey(form.DiscriminatorValue) && Named(form.Record, form.DiscriminatorValue) is { } carrier)
                holder.Add(form.DiscriminatorValue, Blank(carrier));

            return holder;
        }

        /// <summary>Adds the keys a shape requires and the object does not hold. <paramref name="skip"/> is
        /// the field naming the shapes, whose value is written by the caller and never blanked.</summary>
        private static JObject Filled(JObject holder, RecordSchema shape, string? skip)
        {
            foreach (FieldSchema field in shape.Fields)
            {
                if (!field.Required) continue;
                if (string.Equals(field.JsonName, skip, StringComparison.Ordinal)) continue;
                if (holder.ContainsKey(field.JsonName)) continue;

                holder.Add(field.JsonName, Blank(field));
            }

            return holder;
        }

        /// <summary>The keys the outgoing shape names and the incoming one does not. Everything else the
        /// record holds stays: a key both shapes name keeps the value the author gave it, and a key the
        /// schema never named is not the tool's to drop.</summary>
        private static List<string> Shed(VariantSchema? worn, VariantSchema form)
        {
            List<string> shed = [];

            if (worn is null) return shed;

            HashSet<string> kept = new(StringComparer.Ordinal);

            foreach (FieldSchema field in form.Record.Fields) kept.Add(field.JsonName);
            foreach (FieldSchema field in worn.Record.Fields)
                if (!kept.Contains(field.JsonName))
                    shed.Add(field.JsonName);

            return shed;
        }

        private static FieldSchema? Named(RecordSchema record, string jsonName)
        {
            foreach (FieldSchema field in record.Fields)
                if (string.Equals(field.JsonName, jsonName, StringComparison.Ordinal))
                    return field;

            return null;
        }

        /// <summary>The first member of an enum, which is the only value of one that can be picked without
        /// asking. An enum the schema listed no members for has none, and stands empty.</summary>
        private static JToken Member(FieldSchema field) =>
            new JValue(field.EnumValues.Count > 0 ? field.EnumValues[0] : string.Empty);

        private static JToken Nested(FieldSchema field) => field.Record is { } record ? Blank(record) : new JObject();

        /// <summary>A schema's default as a json value. Read by the type it arrived as rather than through
        /// a converter: what the file writes for a whole number and for a fraction are two spellings, and
        /// a default that changed kind on its way in would write the other one.</summary>
        private static JValue? Chosen(object? value) => value switch
        {
            string text => new JValue(text),
            bool flag => new JValue(flag),
            float number => new JValue(number),
            double number => new JValue(number),
            decimal number => new JValue(number),
            sbyte or byte or short or ushort or int or uint or long =>
                new JValue(Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            _ => null
        };
    }
}
