namespace LastBreath.Descriptors.Preview
{
    using System.Collections.Generic;

    /// <summary>
    /// What a record would read as in the game: the title over it and everything under that, already
    /// worded by the game's own formatters. Text and nothing else — a preview is drawn by whoever asked
    /// for it, and a panel that had to know which part is a stat and which is a rolled line would be a
    /// second assembly of the card.
    /// </summary>
    /// <param name="Title">The name the record is shown under; empty where the preview is only a note.</param>
    /// <param name="Lines">The body, one line at a time, in reading order.</param>
    public sealed record PreviewText(string Title, IReadOnlyList<string> Lines)
    {
        /// <summary>A preview that could not be built, saying why. Not an exception: a record half typed
        /// is the state an author spends most of his time in, and a tool that throws at it is one he
        /// closes.</summary>
        public static PreviewText Says(string note) => new(string.Empty, [note]);
    }
}
