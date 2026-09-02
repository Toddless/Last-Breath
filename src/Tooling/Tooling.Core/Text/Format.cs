namespace Tooling.Text
{
    using System.Globalization;

    /// <summary>
    /// The one way a message of the tooling is composed. Every note, status line and row is written in
    /// the invariant culture: a count, a number out of a file and a threshold read the same whatever
    /// the machine the tool was opened on is set to.
    /// </summary>
    public static class Format
    {
        /// <summary>Fills a format with its parts. Imported with <c>using static</c> where messages are
        /// written, so a call site reads as the format it is naming.</summary>
        public static string Text(string format, params object?[] parts) =>
            string.Format(CultureInfo.InvariantCulture, format, parts);
    }
}
