namespace Tooling.Json
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>The shape of a canonical file: how far one level indents and how many decimals a fractional
    /// number keeps.</summary>
    public sealed record CanonicalJsonOptions
    {
        public const int DefaultIndent = 4;

        public const int DefaultFloatDecimals = 4;

        /// <summary>Past this <see cref="Math.Round(double, int)"/> refuses the request, and a double has
        /// nothing left to say anyway.</summary>
        public const int MaxFloatDecimals = 15;

        public static CanonicalJsonOptions Default { get; } = new();

        public int Indent
        {
            get;

            init => field = value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), value, "an indent is a number of spaces");
        } = DefaultIndent;

        /// <summary>Refused rather than clamped: a number nobody can honour is a mistake in whatever built
        /// the options, and a file quietly written to other precision is found much later.</summary>
        public int FloatDecimals
        {
            get;

            init => field = value is >= 0 and <= MaxFloatDecimals
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), value, $"a canonical file keeps between 0 and {MaxFloatDecimals} decimals");
        } = DefaultFloatDecimals;
    }

    /// <summary>Writes a JSON tree the one way this project writes JSON — four spaces, LF, UTF-8 without a
    /// BOM, keys in the order <see cref="IKeyOrder"/> gives — so that loading a canonical file and writing
    /// it back changes not one byte, and a save shows only what the author changed.
    /// <para>A number keeps the kind it had: a whole number is written without a point and a fraction always
    /// with one, so that a file written by the game's own serializer and a file written here agree instead of
    /// pushing "2.0" back and forth between them.</para></summary>
    public static class CanonicalJsonWriter
    {
        private const char IndentSymbol = ' ';
        private const string WindowsNewLine = "\r\n";
        private const string NewLine = "\n";

        private const string WholeNumberFormat = "0";
        private const char DecimalPoint = '.';

        /// <summary>The one decimal a fraction always keeps, so that a token that came in as a fraction goes
        /// out as one: 2.0 stays 2.0 and a save does not turn it into an integer for the next reader.</summary>
        private const char KeptDecimal = '0';

        /// <summary>A decimal written only when it is not a zero, which is what drops "2.50000" to "2.5".</summary>
        private const char OptionalDecimal = '#';

        public static string Write(JToken root, IKeyOrder order, CanonicalJsonOptions options)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(order);
            ArgumentNullException.ThrowIfNull(options);

            JToken canonical = Canonical(root, JsonPointer.Root, order, NumberStyle.For(options.FloatDecimals));
            var builder = new StringBuilder();

            using (var text = new StringWriter(builder, CultureInfo.InvariantCulture))
            using (var writer = new JsonTextWriter(text)
                   {
                       Formatting = Formatting.Indented,
                       Indentation = options.Indent,
                       IndentChar = IndentSymbol,
                       Culture = CultureInfo.InvariantCulture
                   })
            {
                canonical.WriteTo(writer);
            }

            // LF regardless of host, and a newline at the end: the repository is normalized to LF, so a
            // file written here is byte-identical to one written on any other machine.
            return builder.Replace(WindowsNewLine, NewLine).Append(NewLine).ToString();
        }

        public static void WriteFile(string path, JToken root, IKeyOrder order, CanonicalJsonOptions options)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            string canonical = Write(root, order, options);
            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(path, canonical, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        /// <summary>The same tree with its objects reordered and its numbers written the one way. Building a
        /// copy rather than sorting in place keeps the document the author is editing untouched by a save.</summary>
        private static JToken Canonical(JToken token, JsonPointer pointer, IKeyOrder order, NumberStyle style)
        {
            switch (token)
            {
                case JObject source:
                    var holder = new JObject();

                    // OrderBy is stable, so keys of equal rank — every unknown key — keep the file's order.
                    // The address is built only for a child that will be asked about: a scalar never reaches
                    // the key order, and a pointer per number is an allocation per line of the file.
                    foreach (JProperty property in source.Properties().OrderBy(property => order.Rank(pointer, property.Name)))
                        holder.Add(property.Name, Canonical(
                            property.Value,
                            property.Value is JObject or JArray ? pointer.Append(property.Name) : pointer,
                            order,
                            style));

                    return holder;

                case JArray source:
                    var array = new JArray();

                    for (int index = 0; index < source.Count; index++)
                        array.Add(Canonical(
                            source[index],
                            source[index] is JObject or JArray ? pointer.Append(index) : pointer,
                            order,
                            style));

                    return array;

                // Written raw because the default rendering of a double is not the one canon asks for: it
                // keeps every digit the parse produced and has no say in how many the file should hold.
                case JValue { Type: JTokenType.Float } number:
                    return new JRaw(Rounded(number, style));

                default:
                    return token.DeepClone();
            }
        }

        private static string Rounded(JValue number, NumberStyle style)
        {
            if (number.Value is decimal exact)
            {
                decimal value = Math.Round(exact, style.Decimals);

                return NoMinusZero(value).ToString(style.Format, CultureInfo.InvariantCulture);
            }

            double raw = Convert.ToDouble(number.Value, CultureInfo.InvariantCulture);

            if (!double.IsFinite(raw))
                throw new ArgumentOutOfRangeException(nameof(number), raw, "JSON has no way to write this number");

            return NoMinusZero(Math.Round(raw, style.Decimals)).ToString(style.Format, CultureInfo.InvariantCulture);
        }

        /// <summary>Zero has one spelling. A value that rounded down to nothing keeps its sign otherwise, and
        /// "-0" would sit in the diff of a file whose number merely got smaller.</summary>
        private static double NoMinusZero(double value) => value == 0d ? 0d : value;

        private static decimal NoMinusZero(decimal value) => value == 0m ? 0m : value;

        /// <summary>How a fraction is written: the rounding it takes, and a format that keeps the point with
        /// one digit behind it and drops the zeros the rounding left after that.</summary>
        private readonly record struct NumberStyle(int Decimals, string Format)
        {
            public static NumberStyle For(int decimals) =>
                new(decimals, WholeNumberFormat + DecimalPoint + KeptDecimal + new string(OptionalDecimal, Math.Max(decimals - 1, 0)));
        }
    }
}
