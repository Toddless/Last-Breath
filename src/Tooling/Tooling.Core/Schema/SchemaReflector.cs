namespace Tooling.Schema.Reflection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>What the walk could not answer with certainty, in the order it was met. The library has no
    /// tracker of its own, so whoever asked for a schema reads these and reports them its own way.</summary>
    public sealed class SchemaReflectionReport
    {
        private readonly List<string> _notes = [];

        public IReadOnlyList<string> Notes => _notes;

        /// <summary>Nothing to say: every type the walk met was read whole.</summary>
        public bool IsClean => _notes.Count == 0;

        public void Note(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            _notes.Add(text);
        }

        public override string ToString() => string.Join(Environment.NewLine, _notes);
    }

    /// <summary>The one place a CLR type becomes a <see cref="FieldKind"/>. A type the game starts writing
    /// into its catalogs is added here and nowhere else.</summary>
    public static class ClrFieldKinds
    {
        private static readonly Dictionary<Type, FieldKind> s_scalars = new()
        {
            [typeof(string)] = FieldKind.String,
            [typeof(char)] = FieldKind.String,
            [typeof(Guid)] = FieldKind.String,
            [typeof(Uri)] = FieldKind.String,
            [typeof(DateTime)] = FieldKind.String,
            [typeof(DateTimeOffset)] = FieldKind.String,
            [typeof(TimeSpan)] = FieldKind.String,
            [typeof(bool)] = FieldKind.Boolean,
            [typeof(sbyte)] = FieldKind.Integer,
            [typeof(byte)] = FieldKind.Integer,
            [typeof(short)] = FieldKind.Integer,
            [typeof(ushort)] = FieldKind.Integer,
            [typeof(int)] = FieldKind.Integer,
            [typeof(uint)] = FieldKind.Integer,
            [typeof(long)] = FieldKind.Integer,
            [typeof(ulong)] = FieldKind.Integer,
            [typeof(float)] = FieldKind.Number,
            [typeof(double)] = FieldKind.Number,
            [typeof(decimal)] = FieldKind.Number
        };

        /// <summary>Types the tool keeps verbatim instead of describing: the json tokens a DTO holds a free
        /// structure in, and a property that says only "something".</summary>
        public static bool IsFreeForm(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return type == typeof(object) || typeof(JToken).IsAssignableFrom(type);
        }

        public static bool TryScalar(Type type, out FieldKind kind)
        {
            ArgumentNullException.ThrowIfNull(type);

            return s_scalars.TryGetValue(type, out kind);
        }
    }

    /// <summary>
    /// Reads a DTO into a <see cref="RecordSchema"/>: json names as the game's serializer builds them,
    /// field kinds from the CLR types, and the markup the game writes on its members — recognised by the
    /// names in <see cref="MarkupNames"/>, never by type — on top of both.
    /// <para>Properties are read, in the order the type declares them and base types first, which is the
    /// order a canonical file is written in. A public field is named in the report and left out: the
    /// metadata puts every field of a type before every property of it, so a schema holding both would
    /// state an order no file was ever written in.</para>
    /// <para>Defaults are read off an instance built by the parameterless constructor: a type without one —
    /// a positional record — reports no defaults, and everything it declares that is not nullable reads as
    /// required.</para>
    /// <para>Shapes are applied where the record is BUILT, so a descriptor registers them before it reads
    /// the records they sit in; registering them afterwards is reported and changes nothing.</para>
    /// <para>Documentation is left unsaid: XML docs are not in the assembly at runtime.</para>
    /// </summary>
    public sealed class SchemaReflector : ISchemaBuilder
    {
        private static readonly NamingStrategy s_camelCase = new CamelCaseNamingStrategy();

        private readonly Dictionary<Type, VariantSet> _variants = [];
        private readonly Dictionary<Type, object?> _samples = [];
        private readonly HashSet<Type> _met = [];
        private readonly NullabilityInfoContext _nullability = new();

        public SchemaReflectionReport Report { get; } = new();

        /// <summary>Types shapes were registered for that no walk ever reached. A name misspelt in a
        /// <c>typeof</c> registers shapes nothing will ever wear, and the schema comes out with one record
        /// where the file has several.</summary>
        public IReadOnlyList<Type> ShapesNeverMet => [.. _variants.Keys.Where(shaped => !_met.Contains(shaped))];

        public RecordSchema Record(Type dtoType)
        {
            ArgumentNullException.ThrowIfNull(dtoType);

            return Record(dtoType, []);
        }

        public void Polymorphic(Type dtoType, VariantSet variants)
        {
            ArgumentNullException.ThrowIfNull(dtoType);
            ArgumentNullException.ThrowIfNull(variants);

            // A record already built carries the shapes it had when it was built and nothing else: it is a
            // value, and nothing holds a list of the copies handed out. Building the shapes' OWN records
            // first is how a descriptor writes them down, so only the shaped type having been read is late.
            if (_met.Contains(dtoType)) Note(Notes.LateShapes, dtoType.Name);
            if (!_variants.TryAdd(dtoType, variants)) Note(Notes.ShapesTwice, dtoType.Name);

            _variants[dtoType] = variants;
        }

        /// <summary>The json name the game's serializer writes the member under: the one it was given, or
        /// the camel case the naming strategy makes of its own.</summary>
        private static string JsonName(MemberInfo member)
        {
            string? named = member.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName;

            return string.IsNullOrEmpty(named) ? s_camelCase.GetPropertyName(member.Name, hasSpecifiedName: false) : named;
        }

        /// <summary>The properties a serializer would write, base types first and, within a type, in the
        /// order they are declared. A public field is named on the way past and left out.</summary>
        private IEnumerable<PropertyInfo> Members(Type dtoType, Walk walk)
        {
            const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (Type level in Hierarchy(dtoType))
            {
                foreach (FieldInfo field in level.GetFields(Declared))
                    Note(Notes.PublicField, walk.Owner, field.Name);

                foreach (PropertyInfo property in level.GetProperties(Declared).OrderBy(property => property.MetadataToken))
                    yield return property;
            }
        }

        private static IEnumerable<Type> Hierarchy(Type dtoType)
        {
            if (dtoType.IsInterface) return [.. dtoType.GetInterfaces(), dtoType];

            List<Type> levels = [];

            for (Type? level = dtoType; level != null && level != typeof(object); level = level.BaseType)
                levels.Add(level);

            levels.Reverse();

            return levels;
        }

        private static bool AsDictionary(Type type, out Type key, out Type value)
        {
            foreach (Type contract in Contracts(type))
            {
                if (!contract.IsGenericType) continue;

                Type definition = contract.GetGenericTypeDefinition();
                if (definition != typeof(IDictionary<,>) && definition != typeof(IReadOnlyDictionary<,>)) continue;

                key = contract.GenericTypeArguments[0];
                value = contract.GenericTypeArguments[1];

                return true;
            }

            key = value = typeof(object);

            return false;
        }

        private static bool AsSequence(Type type, out Type element)
        {
            if (type.IsArray)
            {
                element = type.GetElementType() ?? typeof(object);

                return true;
            }

            foreach (Type contract in Contracts(type))
                if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    element = contract.GenericTypeArguments[0];

                    return true;
                }

            element = typeof(object);

            return false;
        }

        private static IEnumerable<Type> Contracts(Type type) => type.IsInterface ? [type, .. type.GetInterfaces()] : type.GetInterfaces();

        private static FieldSchema Choice(string jsonName, Type enumType) =>
            new() { JsonName = jsonName, Kind = FieldKind.Enum, EnumValues = Enum.GetNames(enumType) };

        private static FieldSchema Plain(string jsonName, FieldKind kind) => new() { JsonName = jsonName, Kind = kind };

        /// <summary>The element of a list and the value of a map are written without a name of their own.</summary>
        private static FieldSchema Unnamed(FieldSchema field) => field with { JsonName = FieldSchema.Unnamed };

        private static object? ClrDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        /// <summary>Whether the member states a value of its own where it is declared. Read off the built
        /// instance rather than off the syntax: an initializer is the only thing that can leave a member
        /// holding something other than what the CLR gives it.</summary>
        private static bool HasInitializer(Type type, object? value) => value is not null && !value.Equals(ClrDefault(type));

        /// <summary>What the game reads when the key is absent. Only scalars carry one: an absent object or
        /// list is a shape the editor builds, not a value it writes down.</summary>
        private static object? Written(FieldKind kind, object? value) => kind switch
        {
            FieldKind.Object or FieldKind.Array or FieldKind.Dictionary or FieldKind.Any => null,
            _ => value is Enum member ? member.ToString() : value
        };

        private RecordSchema Record(Type dtoType, HashSet<Type> path)
        {
            if (dtoType.ContainsGenericParameters)
            {
                Note(Notes.OpenRecord, dtoType.Name);

                return new RecordSchema { TypeName = dtoType.Name, Fields = [] };
            }

            if ((dtoType.IsAbstract || dtoType.IsInterface) && !_variants.ContainsKey(dtoType))
                Note(Notes.Shapeless, dtoType.Name);

            _met.Add(dtoType);
            path.Add(dtoType);
            SchemaList<FieldSchema> fields = [.. Fields(dtoType, new Walk(dtoType.Name, path))];
            path.Remove(dtoType);

            RecordSchema record = new() { TypeName = dtoType.Name, Fields = fields };

            return _variants.TryGetValue(dtoType, out VariantSet? variants) ? record with { Variants = variants } : record;
        }

        private List<FieldSchema> Fields(Type dtoType, Walk walk)
        {
            object? sample = Sample(dtoType);
            HashSet<string> written = new(StringComparer.Ordinal);
            List<FieldSchema> fields = [];

            foreach (PropertyInfo member in Members(dtoType, walk))
            {
                if (member.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;
                if (!TryRead(member, sample, walk, out Type type, out object? value)) continue;

                string jsonName = JsonName(member);

                if (!written.Add(jsonName))
                {
                    Note(Notes.TwiceNamed, walk.Owner, jsonName);
                    continue;
                }

                fields.Add(Field(member, jsonName, type, value, walk));
            }

            return fields;
        }

        /// <summary>The property's type and the value it holds on a freshly built instance. False for a
        /// property no json name belongs to — an indexer —, one that cannot be read, and one nothing can be
        /// written to, which is a value worked out from the others rather than a value the file holds.</summary>
        private bool TryRead(PropertyInfo property, object? sample, Walk walk, out Type type, out object? value)
        {
            type = property.PropertyType;
            value = null;

            if (property.GetIndexParameters().Length > 0)
            {
                Note(Notes.Indexer, walk.Owner);

                return false;
            }

            if (property.GetMethod is null or { IsPublic: false })
            {
                Note(Notes.NoGetter, walk.Owner, property.Name);

                return false;
            }

            // A get-only collection is filled where it stands rather than assigned, so the file does write
            // it; a get-only anything else is worked out from the fields around it.
            if (property.SetMethod is null && !FilledInPlace(type))
            {
                Note(Notes.Computed, walk.Owner, property.Name);

                return false;
            }

            value = Held(() => property.GetValue(sample), sample, property, walk);

            return true;
        }

        /// <summary>Whether a deserializer would pour values into what is already there instead of putting
        /// something new in its place: that is what it does with a list and a map, and only with those.</summary>
        private static bool FilledInPlace(Type type)
        {
            Type actual = Nullable.GetUnderlyingType(type) ?? type;

            if (ClrFieldKinds.IsFreeForm(actual) || ClrFieldKinds.TryScalar(actual, out _)) return false;

            return AsDictionary(actual, out _, out _) || AsSequence(actual, out _);
        }

        private object? Held(Func<object?> read, object? sample, MemberInfo member, Walk walk)
        {
            if (sample is null) return null;

            try
            {
                return read();
            }
            catch (Exception broken)
            {
                // A computed member may lean on state a bare instance does not have; its default is simply
                // not knowable, and the walk goes on.
                Note(Notes.Unreadable, walk.Owner, member.Name, broken.Message);

                return null;
            }
        }

        /// <summary>An instance built by the parameterless constructor, kept for as long as the reflector
        /// lives; null when the type has no such constructor and nothing can state its defaults.</summary>
        private object? Sample(Type dtoType)
        {
            if (_samples.TryGetValue(dtoType, out object? cached)) return cached;

            object? sample = null;

            if (Buildable(dtoType))
                try
                {
                    sample = Activator.CreateInstance(dtoType);
                }
                catch (Exception broken)
                {
                    // Anything a constructor throws leaves the type without defaults rather than without a
                    // schema: the shape is still worth reading.
                    Note(Notes.NoSample, dtoType.Name, broken.Message);
                }
            else if (!dtoType.IsAbstract && !dtoType.IsInterface && !dtoType.ContainsGenericParameters)
                // A type nothing describes the shape of says so already; a plain type that simply names its
                // parts in a constructor would otherwise lose every default without a word.
                Note(Notes.NoConstructor, dtoType.Name);

            _samples[dtoType] = sample;

            return sample;
        }

        private static bool Buildable(Type dtoType) =>
            !dtoType.IsAbstract
            && !dtoType.IsInterface
            && !dtoType.ContainsGenericParameters
            && (dtoType.IsValueType || dtoType.GetConstructor(Type.EmptyTypes) != null);

        private FieldSchema Field(PropertyInfo member, string jsonName, Type type, object? value, Walk walk)
        {
            Markup markup = MarkupOf(member, new At(walk.Owner, jsonName));

            Agree(jsonName, type, markup, walk);
            Converted(jsonName, type, member, walk);

            FieldSchema field = Value(jsonName, type, markup, walk);

            return field with
            {
                Required = IsRequired(member, type, value),
                Default = Written(field.Kind, value),
                Hidden = markup.Hidden
            };
        }

        /// <summary>The markup written on one member. The attributes belong to the game, which the tool
        /// does not reference, so each is recognised by the NAME of its type and read by the NAMES of what
        /// it carries — the names in <see cref="MarkupNames"/> and nowhere else.</summary>
        private Markup MarkupOf(MemberInfo member, At at)
        {
            Attribute[] written = [.. member.GetCustomAttributes(inherit: false).OfType<Attribute>()];
            Attribute[] references = Every(written, MarkupNames.CatalogRef);
            Attribute[] keys = Every(written, MarkupNames.DictionaryKey);

            return new Markup(
                [.. references.Select(reference => Target(reference, at)).OfType<ReferenceTarget>()],
                // One catalog calling the empty value legal makes it legal: the field is one field.
                references.Any(reference => Flag(reference, MarkupNames.AllowEmpty, at)),
                One(written, MarkupNames.NotARef) is not null,
                EnumOf(One(written, MarkupNames.EnumOf), at),
                Word(One(written, MarkupNames.LocalizedKey), MarkupNames.Suffix, at),
                Bounds(One(written, MarkupNames.Range), at),
                One(written, MarkupNames.Hidden) is not null,
                Word(One(written, MarkupNames.Discriminator), MarkupNames.Field, at),
                // A key names nothing on purpose in no file: json writes no empty key.
                new Narrowing(
                    keys.Select(key => EnumOf(key, at)).FirstOrDefault(enumType => enumType is not null),
                    // Keys point into a whole catalog: a map is not narrowed to a section by any markup.
                    [.. keys.Select(key => Word(key, MarkupNames.Catalog, at)).OfType<string>().Select(ReferenceTarget.Whole)],
                    AllowEmpty: false));
        }

        /// <summary>Where one reference points. Null when the markup names no catalog, which is the one
        /// part of it nothing can stand in for.</summary>
        private ReferenceTarget? Target(Attribute reference, At at)
        {
            if (Word(reference, MarkupNames.Catalog, at) is not { } catalog) return null;

            // A reference into a catalog with no name points at nothing there is to point at. The writing
            // side refuses it outright, so this only ever meets markup written elsewhere — and a walk that
            // threw here would lose a whole record over one attribute.
            if (catalog.Trim().Length == 0)
            {
                Note(Notes.NamelessCatalog, at.Owner, at.Field);

                return null;
            }

            string? section = Word(reference, MarkupNames.Section, at);

            if (section is null) return ReferenceTarget.Whole(catalog);
            if (section.Trim().Length > 0) return new ReferenceTarget(catalog, section);

            // A section written as a blank word narrows the reference to a key no file is written under,
            // which would report every id in the field as broken. Read as the whole catalog, and said.
            Note(Notes.NamelessSection, at.Owner, at.Field, catalog);

            return ReferenceTarget.Whole(catalog);
        }

        private static Attribute[] Every(Attribute[] written, string name) =>
            [.. written.Where(attribute => string.Equals(attribute.GetType().Name, name, StringComparison.Ordinal))];

        private static Attribute? One(Attribute[] written, string name) =>
            written.FirstOrDefault(attribute => string.Equals(attribute.GetType().Name, name, StringComparison.Ordinal));

        private string? Word(Attribute? attribute, string property, At at) =>
            Reads(attribute, property, at, out string? word) ? word : null;

        private Type? EnumOf(Attribute? attribute, At at) =>
            Reads(attribute, MarkupNames.EnumType, at, out Type? enumType) ? enumType : null;

        private bool Flag(Attribute? attribute, string property, At at) =>
            Reads(attribute, property, at, out bool flag) && flag;

        private NumericRange? Bounds(Attribute? attribute, At at)
        {
            if (attribute is null) return null;
            if (!Reads(attribute, MarkupNames.Min, at, out double min)) return null;
            if (!Reads(attribute, MarkupNames.Max, at, out double max)) return null;

            return new NumericRange(min, max);
        }

        /// <summary>What one piece of markup carries under the given name. False when it carries nothing
        /// there — the markup saying nothing about it — and false with a word about it when the name is
        /// not one the attribute answers to at all, which is the convention broken.</summary>
        private bool Reads<T>(Attribute? attribute, string property, At at, out T value)
        {
            value = default!;

            if (attribute is null) return false;

            PropertyInfo? carried = attribute.GetType().GetProperty(property);
            object? held = carried?.GetValue(attribute);

            if (held is T typed)
            {
                value = typed;

                return true;
            }

            // Markup allowed to say nothing under a name says it by holding nothing there; anything else
            // is a name the tool reads by and an attribute that does not answer to it.
            if (carried is not null && held is null) return false;

            Note(Notes.MarkupUnreadable, at.Owner, at.Field, attribute.GetType().Name, property);

            return false;
        }

        /// <summary>The kind a value takes, and the markup narrowing it. Markup that says what a value MEANS
        /// travels to the value itself: the catalog a list of ids points into belongs to the ids.</summary>
        private FieldSchema Value(string jsonName, Type type, Markup markup, Walk walk)
        {
            // A closed type hands back closed member types, and an open one is refused before its members
            // are read, so nothing here is ever left with a type argument to fill in.
            Type actual = Nullable.GetUnderlyingType(type) ?? type;

            if (ClrFieldKinds.IsFreeForm(actual)) return Free(jsonName, markup, walk);

            if (ClrFieldKinds.TryScalar(actual, out FieldKind scalar)) return Narrowed(jsonName, scalar, markup, walk);

            if (actual.IsEnum) return Named(jsonName, actual, markup, walk);

            // The element is read under the name of the field holding it, so that anything said about it is
            // said about a field the author can find, and given the empty name the model asks for after.
            // What the keys hold is answered here and travels no further: the values of the map are a
            // separate question, and the markup answering it is the rest of what is written on the field.
            if (AsDictionary(actual, out Type key, out Type item))
                return new FieldSchema
                {
                    JsonName = jsonName,
                    Kind = FieldKind.Dictionary,
                    Key = Keys(jsonName, key, markup, walk),
                    Item = Unnamed(Value(jsonName, item, markup with { Keys = default }, walk))
                };

            if (AsSequence(actual, out Type element))
                return new FieldSchema
                {
                    JsonName = jsonName,
                    Kind = FieldKind.Array,
                    Item = Unnamed(Value(jsonName, element, markup, walk))
                };

            if (typeof(IEnumerable).IsAssignableFrom(actual))
            {
                Note(Notes.Untyped, walk.Owner, jsonName);

                return Free(jsonName, markup, walk);
            }

            return Nested(jsonName, actual, markup, walk);
        }

        private FieldSchema Free(string jsonName, Markup markup, Walk walk)
        {
            Unapplied(walk, jsonName, markup, text: false, number: false);

            return Plain(jsonName, FieldKind.Any);
        }

        private FieldSchema Nested(string jsonName, Type actual, Markup markup, Walk walk)
        {
            Unapplied(walk, jsonName, markup, text: false, number: false);

            if (!walk.Path.Contains(actual)) return new FieldSchema { JsonName = jsonName, Kind = FieldKind.Object, Record = Record(actual, walk.Path) };

            // Reading it again would read it forever. The field keeps its kind, so the editor still knows an
            // object stands here, and the note says whose shape is missing.
            Note(Notes.Cycle, walk.Owner, jsonName, actual.Name);

            return Plain(jsonName, FieldKind.Object);
        }

        /// <summary>A CLR enum answers with its own members; markup naming another one is a disagreement and
        /// the type wins, because the type is what the file is parsed into.</summary>
        private FieldSchema Named(string jsonName, Type actual, Markup markup, Walk walk)
        {
            Unapplied(walk, jsonName, markup.EnumType == actual ? markup with { EnumType = null } : markup, text: false, number: false);

            return Choice(jsonName, actual);
        }

        private FieldSchema Narrowed(string jsonName, FieldKind scalar, Markup markup, Walk walk)
        {
            bool text = scalar == FieldKind.String;
            bool number = scalar is FieldKind.Integer or FieldKind.Number;

            Unapplied(walk, jsonName, markup, text, number);

            FieldSchema field = Plain(jsonName, scalar);

            if (number && markup.Range is { } range) field = field with { Range = range };
            if (!text) return field;

            if (markup.Narrowings > 1) Note(Notes.NarrowedTwice, walk.Owner, jsonName);
            if (markup.Targets.Count > 0 && markup.NotARef) Note(Notes.RefusedAndNamed, walk.Owner, jsonName);

            FieldSchema narrowed = Narrow(jsonName, markup.Holds) ?? Localized(field, markup.LocalizationSuffix);

            // The refusal rides on whatever the text turned out to be: a field marked and reading as
            // unmarked is what a check for "a reference or a refusal" would never find.
            return narrowed with { RefusedAsReference = markup.NotARef };
        }

        /// <summary>What text is narrowed to: the members of an enum, or the ids of records in catalogs.
        /// Null when the markup narrows it to neither. Asked of the value a field holds and of the keys a
        /// map is written under, which are the same question about two different pieces of markup.</summary>
        private static FieldSchema? Narrow(string jsonName, Narrowing narrowing)
        {
            if (narrowing.EnumType is { } members) return Choice(jsonName, members);

            if (narrowing.Targets.Count == 0) return null;

            return new FieldSchema
            {
                JsonName = jsonName,
                Kind = FieldKind.Reference,
                RefTargets = narrowing.Targets,
                AllowEmpty = narrowing.AllowEmpty
            };
        }

        private static FieldSchema Localized(FieldSchema field, string? suffix) =>
            suffix is null ? field : field with { Kind = FieldKind.LocalizedKey, LocalizationSuffix = suffix };

        /// <summary>What the author may write as a key: the members of an enum, the ids of a catalog, or any
        /// word at all. A key of any other type cannot be written to json as it stands, so the keys are left
        /// free and said so.</summary>
        private FieldSchema? Keys(string jsonName, Type key, Markup markup, Walk walk)
        {
            if (key != typeof(string))
            {
                // The type says what the keys are already; markup about them is a second answer to a
                // question that has one, and the type is what the file is parsed into.
                if (markup.Keys.Ways > 0) Note(Notes.KeyAlreadyTyped, walk.Owner, jsonName, key.Name);

                if (key.IsEnum) return Choice(FieldSchema.Unnamed, key);

                Note(Notes.KeyNotAWord, walk.Owner, jsonName, key.Name);

                return null;
            }

            if (markup.Keys.Ways > 1) Note(Notes.KeyNarrowedTwice, walk.Owner, jsonName);

            if (Narrow(FieldSchema.Unnamed, markup.Keys) is { } narrowed) return narrowed;

            // Markup on a map describes what the map HOLDS. An author who wrote it meaning the keys gets a
            // word about it here, and a puzzling complaint about the values not being text just below.
            if (markup.Narrowings > 0) Note(Notes.MarkupOnValues, walk.Owner, jsonName);

            return null;
        }

        /// <summary>Markup the value it was written on cannot use. Said out loud: an attribute that does
        /// nothing looks exactly like a field nobody has marked up yet.</summary>
        private void Unapplied(Walk walk, string jsonName, Markup markup, bool text, bool number)
        {
            if (!text && markup.Narrowings > 0) Note(Notes.NotAString, walk.Owner, jsonName);
            if (!number && markup.Range is not null) Note(Notes.NotANumber, walk.Owner, jsonName);

            // A map is the one thing read for its keys, and a map never comes through here.
            if (markup.Keys.Ways > 0) Note(Notes.NotAMap, walk.Owner, jsonName);
        }

        /// <summary>Whether the field the markup names as telling shapes apart is the one the shapes
        /// registered for that type are actually told apart by. Silent when no shapes are registered: a
        /// descriptor is free to register them after it has read the record they sit in.</summary>
        private void Agree(string jsonName, Type type, Markup markup, Walk walk)
        {
            if (markup.Discriminator is not { } discriminator) return;

            Type carrier = Carried(type);

            if (!_variants.TryGetValue(carrier, out VariantSet? variants)) return;
            if (string.Equals(variants.Discriminator, discriminator, StringComparison.Ordinal)) return;

            Note(Notes.Disagrees, walk.Owner, jsonName, discriminator, carrier.Name, variants.Discriminator ?? Notes.NoDiscriminator);
        }

        /// <summary>What a field ultimately holds one of: the shapes are the records', and the records sit
        /// however many lists and maps deep the DTO puts them.</summary>
        private static Type Carried(Type type)
        {
            Type carrier = Nullable.GetUnderlyingType(type) ?? type;

            while (!ClrFieldKinds.IsFreeForm(carrier) && !ClrFieldKinds.TryScalar(carrier, out _))
            {
                if (AsDictionary(carrier, out _, out Type value)) carrier = value;
                else if (AsSequence(carrier, out Type element)) carrier = element;
                else break;
            }

            return carrier;
        }

        /// <summary>
        /// Whether a converter stands between the type and the file. The schema describes the DTO, and a
        /// converter is free to write something else entirely — a position that is an id OR a group of
        /// augments, a range that is one number OR a pair — so the shape here is a guess and says so.
        /// </summary>
        /// <remarks>An enum written as its name is the one converter that changes nothing: the schema
        /// already says a name is what the file holds.</remarks>
        private void Converted(string jsonName, Type type, PropertyInfo member, Walk walk)
        {
            Type actual = Nullable.GetUnderlyingType(type) ?? type;

            Type? converter = member.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType
                              ?? actual.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType;

            if (converter is null || converter == typeof(StringEnumConverter)) return;

            Note(Notes.Converted, walk.Owner, jsonName, converter.Name);
        }

        private bool IsRequired(PropertyInfo member, Type type, object? value)
        {
            if (member.GetCustomAttribute<RequiredMemberAttribute>() != null) return true;
            if (Nullable.GetUnderlyingType(type) != null) return false;

            return _nullability.Create(member).ReadState != NullabilityState.Nullable && !HasInitializer(type, value);
        }

        private void Note(string format, params object?[] parts) => Report.Note(Text(format, parts));

        /// <summary>Where the walk stands: the record whose fields are being read, and the types it is
        /// already inside of, which is what a type leading back to itself is recognised by.</summary>
        private readonly record struct Walk(string Owner, HashSet<Type> Path);

        /// <summary>The field a piece of markup was written on, as a note names it.</summary>
        private readonly record struct At(string Owner, string Field);

        /// <summary>What text is narrowed to, whichever piece of markup says so: the members of an enum, or
        /// the ids of records in catalogs. Both cannot be true of one string at once.</summary>
        private readonly record struct Narrowing(Type? EnumType, SchemaList<ReferenceTarget> Targets, bool AllowEmpty)
        {
            /// <summary>How many answers the markup gives. More than one is a contradiction.</summary>
            public int Ways => (EnumType is null ? 0 : 1) + (Targets.Count > 0 ? 1 : 0);
        }

        /// <summary>The schema markup written on one member, read once.</summary>
        private readonly record struct Markup(
            SchemaList<ReferenceTarget> Targets,
            bool AllowEmpty,
            bool NotARef,
            Type? EnumType,
            string? LocalizationSuffix,
            NumericRange? Range,
            bool Hidden,
            string? Discriminator,
            Narrowing Keys)
        {
            /// <summary>How many ways the markup says to read the text. More than one is a contradiction.</summary>
            public int Narrowings =>
                (EnumType is null ? 0 : 1) + (Targets.Count > 0 ? 1 : 0) + (LocalizationSuffix is null ? 0 : 1);

            /// <summary>What the markup narrows the VALUE to. A refusal empties the targets: a validator
            /// told to check ids it should not would report every value in the file as broken.</summary>
            public Narrowing Holds => new(EnumType, NotARef ? SchemaList<ReferenceTarget>.Empty : Targets, AllowEmpty);
        }

        /// <summary>What the reflector has to say about a type it could not read whole.</summary>
        private static class Notes
        {
            public const string NoDiscriminator = "the presence of a key";

            public const string Cycle = "'{0}.{1}' leads back to '{2}', which is already being read: the field stays an object with no record of its own.";
            public const string OpenRecord = "'{0}' has no type arguments, so it was read as a record with no fields.";
            public const string Shapeless = "'{0}' cannot be built and no shapes are registered for it: only the members it declares were read.";
            public const string NoGetter = "'{0}.{1}' cannot be read, so it is not in the schema.";
            public const string Computed = "'{0}.{1}' is worked out from other fields and is not written to the file; it is not in the schema.";
            public const string Converted = "'{0}.{1}' is read through '{2}', so the shape of the file may differ from the shape of the type: state it with variants.";
            public const string LateShapes = "the shapes of '{0}' were registered after records had already been read; the ones built before that did not get them.";
            public const string ShapesTwice = "the shapes of '{0}' were registered twice, and the later set replaced the earlier one.";
            public const string NoConstructor = "'{0}' cannot be built without arguments, so none of its fields carry a default.";
            public const string PublicField = "'{0}.{1}' is a public field, and only properties are read into a schema.";
            public const string MarkupOnValues = "'{0}.{1}' is a map, so the markup on it was read as describing its VALUES; its keys are left free.";
            public const string Indexer = "'{0}' has an indexer, which no json name belongs to; it is not in the schema.";
            public const string Unreadable = "'{0}.{1}' threw when it was read for its default, so it has none: {2}";
            public const string NoSample = "'{0}' cannot be built without arguments, so none of its fields carry a default: {1}";
            public const string TwiceNamed = "'{0}' writes two members under '{1}'; the second is not in the schema.";
            public const string Untyped = "'{0}.{1}' is a sequence that does not say what it holds: it is kept as free-form json.";
            public const string KeyNotAWord = "'{0}.{1}' is keyed by '{2}', which is not a word json can key by: the keys are left free.";
            public const string KeyAlreadyTyped = "'{0}.{1}' is keyed by '{2}' rather than by words, so the markup saying what its keys hold was not applied.";
            public const string KeyNarrowedTwice = "'{0}.{1}' is keyed both by an enum and by a catalog; the enum was taken.";
            public const string NotAMap = "'{0}.{1}' is not a map, so the markup saying what its keys hold was not applied.";
            public const string NarrowedTwice = "'{0}.{1}' is marked as more than one of enum, reference and localization key.";
            public const string NotAString = "'{0}.{1}' is not text, so the markup saying what its text means was not applied.";
            public const string NotANumber = "'{0}.{1}' is not a number, so its range was not applied.";
            public const string RefusedAndNamed = "'{0}.{1}' is marked both a reference and not one; the refusal was taken.";
            public const string Disagrees = "'{0}.{1}' says '{2}' tells the shapes apart, while the shapes registered for '{3}' are told apart by '{4}'.";
            public const string MarkupUnreadable = "'{0}.{1}' carries a '{2}' with no '{3}' the tool can read: that part of the markup was not applied.";
            public const string NamelessSection = "'{0}.{1}' narrows its reference into '{2}' to a section with no name: it points into the whole catalog.";
            public const string NamelessCatalog = "'{0}.{1}' points into a catalog with no name, which is no catalog: the reference was not read.";
        }
    }
}
