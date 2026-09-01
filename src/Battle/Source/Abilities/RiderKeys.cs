namespace Battle.Source.Abilities
{
    /// <summary>
    /// The name a seated augment's rider is filed under, in one place because two things read it: the
    /// augment that seats and unseats its own rider, and the ability answering which copy's riders are
    /// still on it.
    ///
    /// <para>Per COPY and not per record: a second copy is a second purchase, so it rides a second time
    /// on every impact and is pulled on its own. The record's name leads so the seating stays readable
    /// while debugging; the copy's instance is what makes it unique.</para>
    /// </summary>
    internal static class RiderKeys
    {
        private const char Separator = '#';

        internal static string Of(string riderId, string augmentInstanceId) =>
            $"{riderId}{Separator}{augmentInstanceId}";

        /// <summary>Whether that seating belongs to that copy.</summary>
        internal static bool BelongTo(string key, string augmentInstanceId)
        {
            int separator = key.LastIndexOf(Separator);
            return separator >= 0
                   && string.CompareOrdinal(key, separator + 1, augmentInstanceId, 0, augmentInstanceId.Length) == 0
                   && key.Length - separator - 1 == augmentInstanceId.Length;
        }
    }
}
