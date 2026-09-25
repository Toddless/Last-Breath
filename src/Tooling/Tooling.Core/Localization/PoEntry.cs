namespace Tooling.Localization
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// One piece of a PO file in the order the file has it. A catalog is a list of these and nothing else,
    /// so anything the tool does not understand still comes back out where it went in.
    /// </summary>
    public abstract class PoBlock
    {
        internal abstract void WriteTo(List<string> lines);
    }

    /// <summary>
    /// Lines that belong to no entry — a section banner, the blank line between two entries. Kept verbatim
    /// and never rewritten: the banners are the only map the file has, and nothing here is the tool's to edit.
    /// </summary>
    public sealed class PoTextBlock : PoBlock
    {
        private readonly List<string> _lines;

        internal PoTextBlock(IEnumerable<string> lines) => _lines = [.. lines];

        public IReadOnlyList<string> Lines => _lines;

        /// <summary>Nothing but blank lines: the separator a new entry needs beside it, and the one a
        /// removed entry has to take with it so the file does not gain an empty line per deletion.</summary>
        public bool IsBlank => _lines.All(string.IsNullOrWhiteSpace);

        internal override void WriteTo(List<string> lines) => lines.AddRange(_lines);
    }

    /// <summary>One translation of a plural entry, at the index the file gave it.</summary>
    public readonly record struct PoPluralForm(int Index, string Text, bool Multiline);

    /// <summary>
    /// One catalog entry: the key, its translation, and the comment lines standing above it. The lines it
    /// was read from are kept, and an entry nobody changed is written back from them rather than composed
    /// again — the file has spellings a serializer cannot guess, and a diff of untouched entries is noise
    /// the author has to read through to find their own change.
    /// </summary>
    public sealed class PoEntry : PoBlock
    {
        internal PoEntry()
        {
        }

        /// <summary>The comment lines above the entry, with their marks: <c>#</c> from the translator,
        /// <c>#.</c> from the author of the string, <c>#:</c> a reference, <c>#,</c> a flag.</summary>
        public IReadOnlyList<string> Comments { get; internal init; } = [];

        /// <summary>Where the entry started in the file it was read from, for warnings and messages. Zero
        /// for an entry the tool has just added.</summary>
        public int Line { get; internal init; }

        public string MsgId { get; internal set; } = string.Empty;

        /// <summary>The English plural of the key, on the few entries gettext counts with.</summary>
        public string? MsgIdPlural { get; internal init; }

        public string MsgStr { get; internal set; } = string.Empty;

        public IReadOnlyList<PoPluralForm> PluralForms { get; internal init; } = [];

        /// <summary>The entry carrying the file's own settings — charset, plural rule, language. It has no
        /// key, so nothing that addresses entries by key can name it, and nothing may remove it.</summary>
        public bool IsHeader => MsgId.Length == 0;

        public bool HasPlurals => MsgIdPlural is not null;

        /// <summary>Whether this locale actually says something here. A plural entry counts only with every
        /// form filled in: a half-translated plural reads worse in game than an untranslated one.</summary>
        public bool IsTranslated => HasPlurals
            ? PluralForms.Count > 0 && PluralForms.All(form => form.Text.Length > 0)
            : MsgStr.Length > 0;

        internal bool MsgIdMultiline { get; init; }

        internal bool MsgIdPluralMultiline { get; init; }

        internal bool MsgStrMultiline { get; init; }

        /// <summary>The lines the entry was read from, or null for one the tool made up.</summary>
        internal IReadOnlyList<string>? SourceLines { get; init; }

        internal string SourceMsgId { get; init; } = string.Empty;

        internal string SourceMsgStr { get; init; } = string.Empty;

        /// <summary>Whether the entry still says exactly what its lines say. An edit taken back lands here
        /// again, so an undo restores the file and not merely the values.</summary>
        private bool UsesSource => SourceLines is not null
            && MsgId == SourceMsgId
            && MsgStr == SourceMsgStr;

        internal override void WriteTo(List<string> lines)
        {
            lines.AddRange(Comments);

            if (UsesSource)
            {
                lines.AddRange(SourceLines!);
                return;
            }

            PoWriter.WriteValue(lines, PoSyntax.MsgId, MsgId, MsgIdMultiline);

            if (MsgIdPlural is null)
            {
                PoWriter.WriteValue(lines, PoSyntax.MsgStr, MsgStr, MsgStrMultiline);
                return;
            }

            PoWriter.WriteValue(lines, PoSyntax.MsgIdPlural, MsgIdPlural, MsgIdPluralMultiline);

            foreach (PoPluralForm form in PluralForms)
            {
                PoWriter.WriteValue(lines, PoWriter.PluralKeyword(form.Index), form.Text, form.Multiline);
            }
        }
    }
}
