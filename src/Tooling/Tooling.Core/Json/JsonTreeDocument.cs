namespace Tooling.Json
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Editing.History;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The file an authoring tool edits: a JSON tree addressed by <see cref="JsonPointer"/>, changed only
    /// through commands on the undo stack, and written back canonically. Keys this build does not know
    /// are carried through untouched — the tool edits a document, not a set of fields it understands.
    /// </summary>
    public sealed class JsonTreeDocument
    {
        private const string SetLabel = "set";
        private const string InsertLabel = "insert";
        private const string RemoveLabel = "remove";
        private const string MoveLabel = "move";
        private const string RenameLabel = "rename";

        /// <summary>What a step names when it changed the whole document, which has no address of its own.</summary>
        private const string RootLabel = "document";

        private JsonTreeDocument(JToken root, EditHistory? history)
        {
            Root = root;
            History = history ?? new EditHistory();
        }

        /// <summary>Whichever node changed, or the container whose contents shifted. Everything outside —
        /// an inspector, a tree view, a validator — learns about a change from here, whether it came from an
        /// edit or from a step through the history.</summary>
        public event Action<JsonPointer>? Changed;

        public JToken Root { get; private set; }

        /// <summary>The stack this document's edits are filed on — the tool's own where it was handed one,
        /// so that one press of undo takes back the author's last change whichever file it was in, and a
        /// stack of its own where nobody said otherwise.</summary>
        public EditHistory History { get; private set; }

        /// <summary>Whether the tree matches what was last written to disk.</summary>
        public bool IsClean => History.IsCleanFor(this);

        /// <summary>Reads a document, filing its edits on <paramref name="history"/> where one is given.</summary>
        public static JsonTreeDocument Parse(string json, EditHistory? history = null)
        {
            ArgumentNullException.ThrowIfNull(json);

            using var text = new StringReader(json);

            return new JsonTreeDocument(ReadRoot(text), history);
        }

        public static JsonTreeDocument Load(string path, EditHistory? history = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (!File.Exists(path)) throw new FileNotFoundException($"no file at {path}", path);

            // The byte order mark is dropped on the way in and never written on the way out, so a file that
            // arrived with one comes to canon on its first save.
            using var text = new StreamReader(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true);

            return new JsonTreeDocument(ReadRoot(text), history);
        }

        /// <summary>Puts this document on another stack, bringing the steps already taken on it along. A
        /// file the run lays down is written into before it joins the tool: its first step is the one that
        /// created it, and a step left behind on a stack nothing steps is a gesture nobody can take back.</summary>
        public void Follow(EditHistory history)
        {
            ArgumentNullException.ThrowIfNull(history);

            if (ReferenceEquals(history, History)) return;

            history.Take(History);
            History = history;
        }

        public JToken? Resolve(JsonPointer pointer)
        {
            ArgumentNullException.ThrowIfNull(pointer);

            return pointer.Resolve(Root);
        }

        /// <summary>Replaces the value at <paramref name="pointer"/>, merging with the step before it when
        /// that step wrote the same pointer — a run of keystrokes in one field is one thing to take back.
        /// True when the tree holds what was asked for, including when it already did; false when the tree
        /// has nothing there, or when the root is asked to become a value.</summary>
        public bool SetValue(JsonPointer pointer, JToken value)
        {
            ArgumentNullException.ThrowIfNull(pointer);
            ArgumentNullException.ThrowIfNull(value);

            JToken? current = pointer.Resolve(Root);

            if (current is null) return false;
            if (pointer.IsRoot && value is not (JObject or JArray)) return false;

            // Writing what already stands is not an edit: a step filed for it would leave the document
            // reported as unsaved with nothing to save, and one press of undo would take back nothing.
            if (Unchanged(current, value)) return true;

            JToken before = current.DeepClone();
            JToken after = value.DeepClone();

            void Apply(JToken written) => Replace(pointer, written);

            Apply(after);
            History.Record(new ValueEdit<JToken>(new EditTarget(this, pointer.ToString()), before, after, Apply, Labelled(SetLabel, pointer)));

            return true;
        }

        /// <summary>Adds a key to the object at <paramref name="parent"/>. False when there is no object
        /// there, when the key is blank, or when it is already taken — replacing a value is
        /// <see cref="SetValue"/>, and letting an insert do it too would hide the old value from the undo.</summary>
        public bool Insert(JsonPointer parent, string key, JToken value)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(value);

            if (string.IsNullOrEmpty(key)) return false;
            if (parent.Resolve(Root) is not JObject holder || holder.ContainsKey(key)) return false;

            JToken inserted = value.DeepClone();

            void Add()
            {
                RequiredObject(parent).Add(key, inserted.DeepClone());
                Changed?.Invoke(parent);
            }

            void Drop()
            {
                RemoveKey(RequiredObject(parent), key);
                Changed?.Invoke(parent);
            }

            Add();
            History.Record(new TreeEdit(this, Labelled(InsertLabel, parent.Append(key)), Drop, Add));

            return true;
        }

        /// <summary>Puts a value into the array at <paramref name="parent"/>, at the end when the index is
        /// its length. False when there is no array there or the index is outside it.</summary>
        public bool Insert(JsonPointer parent, int index, JToken value)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(value);

            if (parent.Resolve(Root) is not JArray array || index < 0 || index > array.Count) return false;

            JToken inserted = value.DeepClone();

            void Add()
            {
                RequiredArray(parent).Insert(index, inserted.DeepClone());
                Changed?.Invoke(parent);
            }

            void Drop()
            {
                RequiredArray(parent).RemoveAt(index);
                Changed?.Invoke(parent);
            }

            Add();
            History.Record(new TreeEdit(this, Labelled(InsertLabel, parent.Append(index)), Drop, Add));

            return true;
        }

        /// <summary>Puts a value at an address whose key the tree may not hold yet, writing the key when it
        /// does not: the reading the first edit of an absent field makes. One step of the history either
        /// way, so an author who cannot see whether the file held the key never has to know which.</summary>
        public bool Put(JsonPointer pointer, JToken value)
        {
            ArgumentNullException.ThrowIfNull(pointer);
            ArgumentNullException.ThrowIfNull(value);

            return Resolve(pointer) is null && pointer.Parent is { } holder && pointer.Last is { } key
                ? Insert(holder, key, value)
                : SetValue(pointer, value);
        }

        /// <summary>Takes the node out, remembering where it stood so the undo puts it back in its place and
        /// not merely back in the file. False at the root and on a path the tree does not have.</summary>
        public bool Remove(JsonPointer pointer)
        {
            ArgumentNullException.ThrowIfNull(pointer);

            if (pointer.IsRoot || pointer.Resolve(Root) is not { } current) return false;

            JsonPointer parent = pointer.Parent!;
            JToken removed = current.DeepClone();

            switch (parent.Resolve(Root))
            {
                case JObject holder:
                    string key = pointer.Last!;
                    int at = IndexOfProperty(holder, key);

                    void DropKey()
                    {
                        RemoveKey(RequiredObject(parent), key);
                        Changed?.Invoke(parent);
                    }

                    void RestoreKey()
                    {
                        InsertProperty(RequiredObject(parent), at, new JProperty(key, removed.DeepClone()));
                        Changed?.Invoke(parent);
                    }

                    DropKey();
                    History.Record(new TreeEdit(this, Labelled(RemoveLabel, pointer), RestoreKey, DropKey));

                    return true;

                case JArray when JsonPointer.TryReadIndex(pointer.Last!, out int index):
                    void DropItem()
                    {
                        RequiredArray(parent).RemoveAt(index);
                        Changed?.Invoke(parent);
                    }

                    void RestoreItem()
                    {
                        RequiredArray(parent).Insert(index, removed.DeepClone());
                        Changed?.Invoke(parent);
                    }

                    DropItem();
                    History.Record(new TreeEdit(this, Labelled(RemoveLabel, pointer), RestoreItem, DropItem));

                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Moves an array element to another place in its own array. False when the node is not in
        /// an array, when the new index is outside it, or when it is the one the node already has.</summary>
        public bool Move(JsonPointer pointer, int newIndex)
        {
            ArgumentNullException.ThrowIfNull(pointer);

            if (pointer.IsRoot) return false;

            JsonPointer parent = pointer.Parent!;

            if (parent.Resolve(Root) is not JArray array) return false;
            if (!JsonPointer.TryReadIndex(pointer.Last!, out int from) || from >= array.Count) return false;
            if (newIndex < 0 || newIndex >= array.Count || newIndex == from) return false;

            void Shift(int source, int destination)
            {
                JArray owner = RequiredArray(parent);
                JToken moved = owner[source];

                moved.Remove();
                owner.Insert(destination, moved);
                Changed?.Invoke(parent);
            }

            Shift(from, newIndex);
            History.Record(new TreeEdit(this, Labelled(MoveLabel, pointer), () => Shift(newIndex, from), () => Shift(from, newIndex)));

            return true;
        }

        /// <summary>Gives the node another key, in the place the old one held. False when the parent is not
        /// an object, when the new key is blank or is the old one, or when it is already taken — a rename
        /// that swallowed a neighbour would lose a value the undo cannot name.</summary>
        public bool RenameKey(JsonPointer pointer, string newKey)
        {
            ArgumentNullException.ThrowIfNull(pointer);

            if (pointer.IsRoot || string.IsNullOrEmpty(newKey)) return false;

            JsonPointer parent = pointer.Parent!;
            string key = pointer.Last!;

            if (string.Equals(key, newKey, StringComparison.Ordinal)) return false;
            if (parent.Resolve(Root) is not JObject holder) return false;
            if (!holder.ContainsKey(key) || holder.ContainsKey(newKey)) return false;

            int at = IndexOfProperty(holder, key);

            void Rename(string from, string to)
            {
                JObject owner = RequiredObject(parent);
                JToken content = (owner[from] ?? throw new InvalidOperationException($"the object at '{Named(parent)}' no longer holds '{from}'")).DeepClone();

                RemoveKey(owner, from);
                InsertProperty(owner, at, new JProperty(to, content));
                Changed?.Invoke(parent);
            }

            Rename(key, newKey);
            History.Record(new TreeEdit(this, Labelled(RenameLabel, pointer), () => Rename(newKey, key), () => Rename(key, newKey)));

            return true;
        }

        public string Write(IKeyOrder order, CanonicalJsonOptions? options = null) =>
            CanonicalJsonWriter.Write(Root, order, options ?? CanonicalJsonOptions.Default);

        /// <summary>Writes the tree and tells the history this is the state on disk, so the tool can answer
        /// whether what is on screen still needs saving.</summary>
        public void Save(string path, IKeyOrder order, CanonicalJsonOptions? options = null)
        {
            CanonicalJsonWriter.WriteFile(path, Root, order, options ?? CanonicalJsonOptions.Default);
            History.MarkSaved(this);
        }

        private static JToken ReadRoot(TextReader text)
        {
            // Values the author did not touch must survive a save untouched: a date-shaped string stays a
            // string rather than becoming a DateTime that writes back in another format, and every
            // fractional number stays a double rather than a decimal that writes back with other digits.
            using var reader = new JsonTextReader(text)
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double
            };

            JToken root;

            try
            {
                // A key written twice and anything standing after the document are refused rather than
                // swallowed: the whole point of the document is that the file survives a save intact, and
                // the tool cannot say it kept what it never read.
                root = JToken.ReadFrom(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    CommentHandling = CommentHandling.Ignore
                });

                if (reader.Read()) throw new FormatException("the file holds something after the document");
            }
            catch (JsonException broken)
            {
                // One kind of refusal for one kind of failure: a caller that had to tell a reader error from
                // a shape error apart would end up catching both anyway.
                throw new FormatException($"the file is not valid JSON: {broken.Message}", broken);
            }

            return root is JObject or JArray
                ? root
                : throw new FormatException($"the document root has to be an object or an array, not {root.Type}");
        }

        private static int IndexOfProperty(JObject holder, string key)
        {
            int at = 0;

            foreach (JProperty property in holder.Properties())
            {
                if (string.Equals(property.Name, key, StringComparison.Ordinal)) return at;

                at++;
            }

            return holder.Count;
        }

        /// <summary>Puts a property at a known place among its neighbours. Order is content in a canonical
        /// file, so an undo that appended instead would leave a diff of its own.</summary>
        private static void InsertProperty(JObject holder, int at, JProperty property)
        {
            JProperty? following = holder.Properties().ElementAtOrDefault(at);

            if (following is null) holder.Add(property);
            else following.AddBeforeSelf(property);
        }

        private static void RemoveKey(JObject holder, string key)
        {
            if (!holder.Remove(key)) throw new InvalidOperationException($"the object no longer holds '{key}'");
        }

        /// <summary>Whether writing <paramref name="value"/> would leave the file as it is. Asked of the
        /// spelling as well as of the value, because <see cref="JToken.DeepEquals"/> counts 12 and 12.0 as
        /// one number while a canonical file writes them differently.</summary>
        private static bool Unchanged(JToken current, JToken value) =>
            JToken.DeepEquals(current, value) && string.Equals(
                current.ToString(Formatting.None),
                value.ToString(Formatting.None),
                StringComparison.Ordinal);

        private static string Labelled(string action, JsonPointer pointer) => $"{action} {Named(pointer)}";

        private static string Named(JsonPointer pointer) => pointer.IsRoot ? RootLabel : pointer.ToString();

        private void Replace(JsonPointer pointer, JToken value)
        {
            // Cloned on every application: the same instance cannot hang in the tree and in the step that
            // restores it, and an edit under it would rewrite what the undo is holding.
            JToken written = value.DeepClone();

            if (pointer.IsRoot) Root = written;
            else Required(pointer).Replace(written);

            Changed?.Invoke(pointer);
        }

        private JToken Required(JsonPointer pointer) =>
            pointer.Resolve(Root) ?? throw new InvalidOperationException($"the document has nothing at '{Named(pointer)}'");

        private JObject RequiredObject(JsonPointer pointer) =>
            Required(pointer) as JObject ?? throw new InvalidOperationException($"'{Named(pointer)}' is no longer an object");

        private JArray RequiredArray(JsonPointer pointer) =>
            Required(pointer) as JArray ?? throw new InvalidOperationException($"'{Named(pointer)}' is no longer an array");

        /// <summary>A structural change that carries both directions it can be applied in. Each of them
        /// closes over the state captured before the change — the node that was taken out, the place it
        /// stood in — and works nothing out again.</summary>
        private sealed class TreeEdit(JsonTreeDocument owner, string label, Action undo, Action redo) : IOwnedEdit
        {
            public string Label => label;

            public void Undo() => undo();

            public void Redo() => redo();

            public bool Touches(object asked) => ReferenceEquals(asked, owner);
        }
    }
}
