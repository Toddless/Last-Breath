namespace Tooling.Json
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A value of a document as the file writes it. <see cref="JToken.ToString()"/> answers in the CLR's
    /// own words and in the culture of the machine — <c>True</c> for a boolean, <c>0,5</c> for a number
    /// on a Russian Windows — which is neither what the file holds nor what an author would type back
    /// in. Everything that shows a value to an author, or takes one for an id, goes through here.
    /// </summary>
    public static class JsonScalars
    {
        /// <summary>The largest whole number a double still holds exactly. Past it a number is no longer
        /// the one it was asked to be, so it is written as the fraction it has become rather than as an
        /// integer that is a different number.</summary>
        public const double ExactWholeLimit = 9007199254740992d;

        /// <summary>The token written as json, except for text, which comes back unquoted: a name is
        /// read, not parsed.</summary>
        public static string Written(JToken value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return value.Type == JTokenType.String ? value.Value<string>()! : value.ToString(Formatting.None);
        }

        /// <summary>
        /// A number that is to stand where <paramref name="previous"/> stands, written as the kind the
        /// file already had. The writer keeps whatever kind it is handed, and the game's own serializer
        /// has a kind of its own for every field: a value that changed kind under an author who only
        /// changed its digits would push "2" and "2.0" back and forth between the two of them forever.
        /// <para><paramref name="whole"/> decides for a key the file does not hold yet — there is no
        /// previous token to take the kind from, so what the field itself holds has the say.</para>
        /// </summary>
        public static JValue Number(JToken? previous, double value, bool whole)
        {
            bool integer = previous switch
            {
                JValue { Type: JTokenType.Integer } => true,
                JValue { Type: JTokenType.Float } => false,
                _ => whole
            };

            return integer && value == Math.Truncate(value) && Math.Abs(value) <= ExactWholeLimit
                ? new JValue((long)value)
                : new JValue(value);
        }
    }
}
