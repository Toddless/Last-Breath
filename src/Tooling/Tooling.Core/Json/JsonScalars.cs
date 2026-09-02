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
        /// <summary>The token written as json, except for text, which comes back unquoted: a name is
        /// read, not parsed.</summary>
        public static string Written(JToken value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return value.Type == JTokenType.String ? value.Value<string>()! : value.ToString(Formatting.None);
        }
    }
}
