namespace Tooling.Localization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Editing.History;

    /// <summary>Something the file said that the document kept but the author has to be told about. A file
    /// the tool cannot keep whole is refused instead; a warning means the text survived and reads oddly.</summary>
    public readonly record struct PoWarning(int Line, string Message);

    /// <summary>
    /// The gettext catalog an authoring tool edits: blocks in the order the file has them, entries addressed
    /// by msgid, changed only through commands on the undo stack. The tool is the second writer of these
    /// files — a hand-edited line it never touched has to survive a save byte for byte.
    /// </summary>
    public sealed class PoDocument
    {
        private const string SetLabel = "set";
        private const string AddLabel = "add";
        private const string RemoveLabel = "remove";
        private const string RenameLabel = "rename";

        /// <summary>What a step names when it changed the settings entry, which has no key of its own.</summary>
        private const string HeaderName = "header";

        private readonly List<PoBlock> _blocks;
        private readonly Dictionary<string, PoEntry> _index = new(StringComparer.Ordinal);
        private readonly List<PoWarning> _warnings;

        private PoDocument(List<PoBlock> blocks, List<PoWarning> warnings, string newLine, bool endsWithNewLine, bool byteOrderMark)
        {
            _blocks = blocks;
            _warnings = warnings;
            NewLine = newLine;
            EndsWithNewLine = endsWithNewLine;
            HasByteOrderMark = byteOrderMark;

            Index();

            // Godot reads charset and plural rule from the header. A file without one still opens, because
            // refusing it would leave the author with no way to add it through the tool.
            if (_blocks.Count > 0 && Header is null) _warnings.Add(new PoWarning(0, "the file has no header entry"));
        }

        /// <summary>The key of the entry that changed, whether the change came from an edit or from a step
        /// through the history. A rename reports the key the entry has after the step.</summary>
        public event Action<string>? Changed;

        public EditHistory History { get; } = new();

        public IReadOnlyList<PoBlock> Blocks => _blocks;

        public IEnumerable<PoEntry> Entries => _blocks.OfType<PoEntry>();

        /// <summary>The keyless entry carrying charset, plural rule and language, when the file has one.</summary>
        public PoEntry? Header => TryGet(string.Empty);

        public IReadOnlyList<PoWarning> Warnings => _warnings;

        /// <summary>The line ending the file arrived with, kept so a save does not rewrite every line. A
        /// file that mixed them is written with the one it used most and says so in the warnings.</summary>
        public string NewLine { get; }

        public bool EndsWithNewLine { get; }

        public bool HasByteOrderMark { get; }

        public bool IsClean => History.IsClean;

        public static PoDocument Parse(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            return Read(text);
        }

        public static PoDocument Load(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (!File.Exists(path)) throw new FileNotFoundException($"no file at {path}", path);

            try
            {
                // Read as bytes and decoded by hand: a reader given an encoding swallows the mark, and a mark
                // swallowed on the way in is a mark missing from the file the tool writes back. The decoder
                // throws rather than substituting, because a byte quietly replaced here stands in the source
                // lines of an entry nobody edited and goes to disk on the next save.
                return Read(Utf8(strict: true).GetString(File.ReadAllBytes(path)));
            }
            catch (DecoderFallbackException broken)
            {
                throw new FormatException($"the file is not valid UTF-8: {broken.Message}", broken);
            }
        }

        public PoEntry? TryGet(string msgId)
        {
            ArgumentNullException.ThrowIfNull(msgId);

            return _index.GetValueOrDefault(msgId);
        }

        /// <summary>The entry standing above <paramref name="msgId"/>, or null when it is the first one.
        /// A key is added next to a neighbour, so several files can put it in the same place.</summary>
        public PoEntry? EntryBefore(string msgId)
        {
            if (TryGet(msgId) is not { } entry) return null;

            PoEntry? previous = null;

            foreach (PoEntry standing in Entries)
            {
                if (ReferenceEquals(standing, entry)) return previous;

                previous = standing;
            }

            return null;
        }

        /// <summary>Writes a translation, merging with the step before it when that step wrote the same key
        /// — a run of keystrokes in one field is one thing to take back. True when the entry holds what was
        /// asked for, including when it already did; false when there is no such key, and false on a plural
        /// entry, whose forms this document does not let anyone edit one at a time.</summary>
        public bool Set(string msgId, string msgStr)
        {
            ArgumentNullException.ThrowIfNull(msgStr);

            if (TryGet(msgId) is not { } entry || entry.HasPlurals) return false;

            // Writing what already stands is not an edit: a step filed for it would leave the file reported
            // as unsaved with nothing to save, and one press of undo would take back nothing.
            if (string.Equals(entry.MsgStr, msgStr, StringComparison.Ordinal)) return true;

            string before = entry.MsgStr;

            void Apply(string written)
            {
                entry.MsgStr = written;
                Changed?.Invoke(entry.MsgId);
            }

            Apply(msgStr);
            History.Record(new ValueEdit<string>(new EditTarget(this, msgId), before, msgStr, Apply, Labelled(SetLabel, entry)));

            return true;
        }

        /// <summary>Puts a new key after <paramref name="afterMsgId"/>, or at the end of the file when that
        /// is null, together with the blank line entries stand apart by. False when the key is blank, when
        /// it is already there, or when the neighbour named is not.</summary>
        public bool Add(string msgId, string msgStr, string? afterMsgId = null)
        {
            ArgumentNullException.ThrowIfNull(msgId);
            ArgumentNullException.ThrowIfNull(msgStr);

            // Asked of the blocks and not of the index: a key the file writes twice is indexed once, and an
            // add that went by the index would put a third copy of it into the file.
            if (msgId.Length == 0 || Holds(msgId)) return false;

            var entry = new PoEntry { MsgId = msgId, MsgStr = msgStr };
            int at;

            if (afterMsgId is null)
            {
                at = _blocks.Count;
            }
            else
            {
                int neighbour = BlockOf(afterMsgId);

                if (neighbour < 0) return false;

                at = neighbour + 1;

                // Past the blank line the neighbour is followed by, so the new entry lands after it and not
                // between it and its own separator.
                if (at < _blocks.Count && IsSeparator(_blocks[at])) at++;
            }

            List<PoBlock> added = Surrounded(entry, at);

            void Put()
            {
                _blocks.InsertRange(at, added);
                Reindex(msgId);
                Changed?.Invoke(msgId);
            }

            void Take()
            {
                _blocks.RemoveRange(at, added.Count);
                Reindex(msgId);
                Changed?.Invoke(msgId);
            }

            Put();
            History.Record(new StructuralEdit($"{AddLabel} {msgId}", Take, Put));

            return true;
        }

        /// <summary>Takes the entry out with the blank line that stood beside it, remembering where both
        /// were so the undo puts the key back in its section and not at the end of the file. False on a key
        /// the file does not have, and on the settings entry, which is not the tool's to delete.</summary>
        public bool Remove(string msgId)
        {
            if (TryGet(msgId) is not { } entry || entry.IsHeader) return false;

            int at = _blocks.IndexOf(entry);
            int start = at;
            List<PoBlock> taken = [entry];

            if (at + 1 < _blocks.Count && IsSeparator(_blocks[at + 1]))
            {
                taken.Add(_blocks[at + 1]);
            }
            else if (at > 0 && IsSeparator(_blocks[at - 1]))
            {
                start = at - 1;
                taken.Insert(0, _blocks[at - 1]);
            }

            void Take()
            {
                _blocks.RemoveRange(start, taken.Count);
                Reindex(msgId);
                Changed?.Invoke(msgId);
            }

            void Put()
            {
                _blocks.InsertRange(start, taken);
                Reindex(msgId);
                Changed?.Invoke(msgId);
            }

            Take();
            History.Record(new StructuralEdit($"{RemoveLabel} {msgId}", Put, Take));

            return true;
        }

        /// <summary>Gives the entry another key in the place the old one held, translation and comments
        /// untouched. False when the old key is not there or is the settings entry, when the new one is
        /// blank or already taken — a rename that swallowed a neighbour would lose a line the undo cannot
        /// name.</summary>
        public bool Rename(string oldId, string newId)
        {
            ArgumentNullException.ThrowIfNull(oldId);
            ArgumentNullException.ThrowIfNull(newId);

            if (newId.Length == 0 || string.Equals(oldId, newId, StringComparison.Ordinal)) return false;
            if (TryGet(oldId) is not { } entry || entry.IsHeader) return false;
            if (Holds(newId)) return false;

            void Name(string from, string to)
            {
                entry.MsgId = to;
                Reindex(from);
                Reindex(to);

                // Both keys: whatever was showing the old one has to stop, and whatever addresses the new one
                // has to start. A view told about one of them holds a row that no longer names anything.
                Changed?.Invoke(from);
                Changed?.Invoke(to);
            }

            Name(oldId, newId);
            History.Record(new RenameEdit($"{RenameLabel} {oldId}", oldId, newId, Name));

            return true;
        }

        public string Write()
        {
            List<string> lines = [];

            foreach (PoBlock block in _blocks)
            {
                block.WriteTo(lines);
            }

            var text = new StringBuilder();

            if (HasByteOrderMark) text.Append(PoSyntax.ByteOrderMark);

            text.AppendJoin(NewLine, lines);

            if (EndsWithNewLine && lines.Count > 0) text.Append(NewLine);

            return text.ToString();
        }

        /// <summary>Writes the file and tells the history this is the state on disk, so the tool can answer
        /// whether what is on screen still needs saving.</summary>
        public void Save(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            File.WriteAllBytes(path, Utf8(strict: false).GetBytes(Write()));
            History.MarkSaved();
        }

        /// <summary>The encoding these files are written in: UTF-8, and the mark, when the file had one, is a
        /// character of the document rather than something the encoder adds.</summary>
        private static UTF8Encoding Utf8(bool strict) =>
            new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: strict);

        private static PoDocument Read(string text)
        {
            List<PoWarning> warnings = [];
            bool byteOrderMark = text.Length > 0 && text[0] == PoSyntax.ByteOrderMark;

            if (byteOrderMark)
            {
                warnings.Add(new PoWarning(1, "the file starts with a byte order mark; it is kept, but gettext files are written without one"));
                text = text[1..];
            }

            string newLine = DetectNewLine(text, warnings);

            // A file with nothing in it yet is still a text file: the first entry the tool puts into it ends
            // with a newline like every other line of every other catalog.
            bool endsWithNewLine = text.Length == 0 || text.EndsWith(PoSyntax.LineFeed);
            string[] lines = SplitLines(text, endsWithNewLine);

            if (lines.Length == 0) warnings.Add(new PoWarning(0, "the file is empty"));

            return new PoDocument(ReadBlocks(lines, warnings), warnings, newLine, endsWithNewLine, byteOrderMark);
        }

        /// <summary>The ending the file used. A file that mixed them cannot be written back untouched either
        /// way, so the one it used most is kept and the mixing is reported.</summary>
        private static string DetectNewLine(string text, List<PoWarning> warnings)
        {
            int feeds = 0;
            int pairs = 0;

            for (int at = 0; at < text.Length; at++)
            {
                if (text[at] != PoSyntax.LineFeed) continue;

                feeds++;

                if (at > 0 && text[at - 1] == PoSyntax.CarriageReturn) pairs++;
            }

            if (pairs == 0) return PoSyntax.Lf;
            if (pairs == feeds) return PoSyntax.CrLf;

            warnings.Add(new PoWarning(0, "the file mixes line endings; it is saved with the one it uses most"));

            return pairs * 2 >= feeds ? PoSyntax.CrLf : PoSyntax.Lf;
        }

        private static string[] SplitLines(string text, bool endsWithNewLine)
        {
            if (text.Length == 0) return [];

            string[] split = text.Split(PoSyntax.LineFeed);
            int count = endsWithNewLine ? split.Length - 1 : split.Length;
            var lines = new string[count];

            for (int at = 0; at < count; at++)
            {
                lines[at] = split[at].TrimEnd(PoSyntax.CarriageReturn);
            }

            return lines;
        }

        private static List<PoBlock> ReadBlocks(string[] lines, List<PoWarning> warnings)
        {
            List<PoBlock> blocks = [];
            List<string> pending = [];
            int at = 0;

            while (at < lines.Length)
            {
                if (IsCommentOrBlank(lines[at]))
                {
                    pending.Add(lines[at]);
                    at++;
                    continue;
                }

                int attached = AttachPoint(pending);

                if (attached > 0) blocks.Add(new PoTextBlock(pending.GetRange(0, attached)));

                List<string> comments = pending.GetRange(attached, pending.Count - attached);
                pending.Clear();
                blocks.Add(ReadEntry(lines, ref at, comments, warnings));
            }

            if (pending.Count > 0) blocks.Add(new PoTextBlock(pending));

            return blocks;
        }

        /// <summary>
        /// Where the comments belonging to the entry below start: after the last blank line, or after the
        /// last bare comment, whichever stands later. A marked comment is written against one key and goes
        /// with it; a bare one is how these files draw their section banners, and an entry that owned a
        /// banner would carry the whole heading away the moment the author deleted that one key.
        /// </summary>
        private static int AttachPoint(List<string> pending)
        {
            int at = pending.Count;

            while (at > 0 && !IsBlank(pending[at - 1]) && !IsFileComment(pending[at - 1])) at--;

            return at;
        }

        private static bool IsCommentOrBlank(string line) => IsBlank(line) || IsComment(line);

        private static bool IsBlank(string line) => string.IsNullOrWhiteSpace(line);

        private static bool IsComment(string line) => Starts(line, PoSyntax.Comment);

        /// <summary>A continuation of the value above it. gettext allows these to be indented, so the line is
        /// read past its blanks — while the raw line still goes into the entry's source lines untouched.</summary>
        private static bool IsContinuation(string line) => Starts(line, PoSyntax.Quote);

        /// <summary>A comment the file owns rather than the entry under it: anything but the marks gettext
        /// writes against a message (<c>#.</c> extracted, <c>#:</c> reference, <c>#,</c> flag, <c>#|</c>
        /// previous, <c>#~</c> obsolete).</summary>
        private static bool IsFileComment(string line)
        {
            ReadOnlySpan<char> text = Content(line);

            return text.Length > 0
                && text[0] == PoSyntax.Comment
                && (text.Length == 1 || !PoSyntax.CommentMarks.Contains(text[1]));
        }

        private static bool Starts(string line, char mark)
        {
            ReadOnlySpan<char> text = Content(line);

            return text.Length > 0 && text[0] == mark;
        }

        private static ReadOnlySpan<char> Content(string line) => line.AsSpan().TrimStart();

        private static PoEntry ReadEntry(string[] lines, ref int at, List<string> comments, List<PoWarning> warnings)
        {
            int start = at + 1;
            List<string> body = [];
            List<PoField> fields = [];

            while (at < lines.Length && !IsCommentOrBlank(lines[at]))
            {
                List<string> read = [];
                int next = at;
                PoField field = ReadField(lines, ref next, read);

                // A second msgid is the entry after this one, standing without a blank line between them.
                if (fields.Count > 0 && field.Keyword == PoSyntax.MsgId) break;

                at = next;
                body.AddRange(read);
                fields.Add(field);
            }

            // A comment where the translation should be belongs to neither side: read as this entry's it
            // would be dropped the first time the entry was rewritten, and read as the next entry's it would
            // silently turn a key into one without a msgstr.
            if (Unfinished(fields) && at < lines.Length && IsComment(lines[at]))
            {
                throw PoWriter.Broken(at + 1, $"a comment stands inside the entry started at line {start}");
            }

            return Compose(fields, body, comments, start, warnings);
        }

        private static bool Unfinished(List<PoField> fields) =>
            fields.Count > 0 && !fields.Any(field => field.Keyword == PoSyntax.MsgStr);

        /// <summary>Reads one keyword and the value under it, which may go on across as many quoted lines as
        /// it likes.</summary>
        private static PoField ReadField(string[] lines, ref int at, List<string> read)
        {
            int number = at + 1;
            string line = lines[at];
            (string keyword, int form, int valueAt) = ReadKeyword(line, number);
            var value = new StringBuilder(PoWriter.ReadQuoted(line, valueAt, number));

            read.Add(line);
            at++;

            bool multiline = false;

            while (at < lines.Length && IsContinuation(lines[at]))
            {
                value.Append(PoWriter.ReadQuoted(lines[at], 0, at + 1));
                read.Add(lines[at]);
                multiline = true;
                at++;
            }

            return new PoField(keyword, form, value.ToString(), multiline, number);
        }

        private static (string Keyword, int Form, int ValueAt) ReadKeyword(string line, int number)
        {
            int from = line.Length - Content(line).Length;
            int end = line.IndexOf(PoSyntax.Space, from);
            string token = end < 0 ? line[from..] : line[from..end];
            int valueAt = end < 0 ? line.Length : end + 1;
            int form = PoSyntax.Singular;

            if (token.StartsWith(PoSyntax.MsgStr + PoSyntax.PluralOpen, StringComparison.Ordinal))
            {
                int close = token.IndexOf(PoSyntax.PluralClose);

                if (close < 0 || !int.TryParse(token[(PoSyntax.MsgStr.Length + 1)..close], out form))
                {
                    throw PoWriter.Broken(number, $"'{token}' is not a plural form of msgstr");
                }

                token = PoSyntax.MsgStr;
            }

            return token switch
            {
                PoSyntax.MsgId or PoSyntax.MsgIdPlural or PoSyntax.MsgStr => (token, form, valueAt),
                PoSyntax.MsgContext => throw PoWriter.Broken(number, "msgctxt is not supported; this document cannot write it back"),
                _ when token.Length > 0 && token[0] == PoSyntax.Quote => throw PoWriter.Broken(number, "a quoted line stands where no value is being continued"),
                _ => throw PoWriter.Broken(number, $"'{token}' is not a gettext keyword")
            };
        }

        private static PoEntry Compose(List<PoField> fields, List<string> body, List<string> comments, int line, List<PoWarning> warnings)
        {
            if (fields.Count == 0 || fields[0].Keyword != PoSyntax.MsgId) throw PoWriter.Broken(line, "an entry has to start with msgid");

            PoField id = fields[0];
            PoField? plural = null;
            PoField? translation = null;
            List<PoPluralForm> forms = [];

            foreach (PoField field in fields.Skip(1))
            {
                switch (field.Keyword)
                {
                    case PoSyntax.MsgIdPlural when plural is null:
                        plural = field;
                        break;

                    case PoSyntax.MsgStr when field.Form == PoSyntax.Singular && translation is null:
                        translation = field;
                        break;

                    case PoSyntax.MsgStr when field.Form != PoSyntax.Singular:
                        forms.Add(new PoPluralForm(field.Form, field.Value, field.Multiline));
                        break;

                    default:
                        throw PoWriter.Broken(field.Line, $"'{field.Keyword}' stands twice in one entry");
                }
            }

            Report(id, plural, translation, forms, warnings);

            return new PoEntry
            {
                Comments = comments,
                Line = line,
                MsgId = id.Value,
                MsgIdMultiline = id.Multiline,
                MsgIdPlural = plural?.Value,
                MsgIdPluralMultiline = plural?.Multiline ?? false,
                MsgStr = translation?.Value ?? string.Empty,
                MsgStrMultiline = translation?.Multiline ?? false,
                PluralForms = forms,
                SourceLines = body,
                SourceMsgId = id.Value,
                SourceMsgStr = translation?.Value ?? string.Empty
            };
        }

        /// <summary>What an entry can say and still be kept: a missing or misplaced translation reads as an
        /// untranslated key rather than stopping the file from opening.</summary>
        private static void Report(PoField id, PoField? plural, PoField? translation, List<PoPluralForm> forms, List<PoWarning> warnings)
        {
            string key = id.Value.Length == 0 ? HeaderName : id.Value;

            if (plural is null && forms.Count > 0)
            {
                warnings.Add(new PoWarning(id.Line, $"'{key}' has plural forms without a msgid_plural"));
            }

            if (plural is not null && forms.Count == 0)
            {
                warnings.Add(new PoWarning(id.Line, $"'{key}' has a msgid_plural without any form"));
            }

            if (plural is null && translation is null)
            {
                warnings.Add(new PoWarning(id.Line, $"'{key}' has no msgstr; it reads as untranslated"));
            }

            for (int at = 0; at < forms.Count; at++)
            {
                if (forms[at].Index == at) continue;

                warnings.Add(new PoWarning(id.Line, $"'{key}' numbers its plural forms out of order"));
                break;
            }
        }

        /// <summary>The one blank line entries stand apart by — and only one. A wider gap is how these files
        /// break a section off from the next, so an add must not step over it into the section beyond and a
        /// remove must not swallow it and weld the two sections together.</summary>
        private static bool IsSeparator(PoBlock block) => block is PoTextBlock { IsBlank: true, Lines.Count: 1 };

        private static bool StartsBlank(PoBlock block) =>
            block is PoTextBlock text && text.Lines.Count > 0 && string.IsNullOrWhiteSpace(text.Lines[0]);

        private static bool EndsBlank(PoBlock block) =>
            block is PoTextBlock text && text.Lines.Count > 0 && string.IsNullOrWhiteSpace(text.Lines[^1]);

        private static PoTextBlock Separator() => new PoTextBlock([string.Empty]);

        private static string Labelled(string action, PoEntry entry) => $"{action} {(entry.IsHeader ? HeaderName : entry.MsgId)}";

        private void Index()
        {
            foreach (PoEntry entry in Entries)
            {
                // gettext takes the first of a repeated key. The second is left in the file so a save keeps
                // it, but nothing addresses it, and the author is told which line to go and look at.
                if (_index.TryAdd(entry.MsgId, entry)) continue;

                _warnings.Add(new PoWarning(entry.Line, $"'{entry.MsgId}' is written again here; the first one is the one that counts"));
            }
        }

        /// <summary>Points the key at the first entry that carries it now, or drops it when none does. Every
        /// edit that moves or renames an entry goes through here: a file that writes one key twice would
        /// otherwise leave the index naming an entry the blocks no longer hold, or naming nothing while the
        /// key is still written in the file.</summary>
        private void Reindex(string msgId)
        {
            _index.Remove(msgId);

            foreach (PoEntry entry in Entries)
            {
                if (!string.Equals(entry.MsgId, msgId, StringComparison.Ordinal)) continue;

                _index[msgId] = entry;
                return;
            }
        }

        private bool Holds(string msgId) => Entries.Any(entry => string.Equals(entry.MsgId, msgId, StringComparison.Ordinal));

        private int BlockOf(string msgId) => TryGet(msgId) is { } entry ? _blocks.IndexOf(entry) : -1;

        /// <summary>The entry together with whichever blank lines it needs to stand apart at that place:
        /// entries are separated by one, and an entry dropped in without them would glue itself to a
        /// neighbour or to a section banner.</summary>
        private List<PoBlock> Surrounded(PoEntry entry, int at)
        {
            List<PoBlock> blocks = [];

            if (at > 0 && !EndsBlank(_blocks[at - 1])) blocks.Add(Separator());

            blocks.Add(entry);

            if (at < _blocks.Count && !StartsBlank(_blocks[at])) blocks.Add(Separator());

            return blocks;
        }

        /// <summary>One keyword and its value as the file spelled them.</summary>
        private readonly record struct PoField(string Keyword, int Form, string Value, bool Multiline, int Line);

        /// <summary>A change of shape that carries both directions it can be applied in. Each of them closes
        /// over the state captured before the change — the entry that was taken out, the place it stood in —
        /// and works nothing out again.</summary>
        private sealed class StructuralEdit(string label, Action undo, Action redo) : IEditCommand
        {
            public string Label => label;

            public void Undo() => undo();

            public void Redo() => redo();
        }

        /// <summary>A rename, which has to say which key became which: a key is how the rest of the tool holds
        /// an entry, so whatever was pointing at the old one points at nothing the moment the step lands.</summary>
        private sealed class RenameEdit(string label, string oldId, string newId, Action<string, string> name) : IIdChangingEdit
        {
            public string Label => label;

            public void Undo() => name(newId, oldId);

            public void Redo() => name(oldId, newId);

            public IdSwap Swap(bool undoing) => undoing ? new IdSwap(newId, oldId) : new IdSwap(oldId, newId);
        }
    }
}
