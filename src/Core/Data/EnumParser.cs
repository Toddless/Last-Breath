namespace Core.Data
{
    using System;

    /// <summary>
    /// The one enum-parsing policy for game data: strict and case-insensitive. A typo in JSON
    /// must fail the file loudly, never silently fall back to the first enum member.
    /// </summary>
    public static class EnumParser
    {
        public static TEnum ParseEnum<TEnum>(string value)
            where TEnum : struct, Enum =>
            TryParseEnum(value, out TEnum result)
                ? result
                : throw new FormatException($"'{value}' is not a valid {typeof(TEnum).Name}");

        /// <summary>For genuinely optional fields: absent value means default, a present value is parsed strictly.</summary>
        public static TEnum ParseEnumOrDefault<TEnum>(string? value)
            where TEnum : struct, Enum =>
            string.IsNullOrEmpty(value) ? default : ParseEnum<TEnum>(value);

        /// <summary>For fields sharing several enum namespaces (EntityParameter/ContextParameter):
        /// probe a branch without failing — the caller reports when no namespace matched.</summary>
        public static bool TryParseEnum<TEnum>(string value, out TEnum result)
            where TEnum : struct, Enum
        {
            result = default;

            return NamesOneMember<TEnum>(value) && Enum.TryParse(value, ignoreCase: true, out result);
        }

        /// <summary>
        /// A member of an ordinary enum is ONE NAME, written out.
        /// <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> is looser than that in two ways, and
        /// both let a data file author a member nobody wrote:
        /// <list type="bullet">
        /// <item>a comma-separated list is accepted for any enum at all and the members are ORed together,
        /// so naming two of them mints the number their bits add up to — a third member, or none;</item>
        /// <item>a bare number is accepted and handed back as the member sitting on it, so "28" names
        /// whichever knob happens to be twenty-eighth today and "999" comes back true carrying a value no
        /// member has at all.</item>
        /// </list>
        /// Nothing downstream can tell any of these from an authored member: what it receives is an
        /// ordinary enum value. Whether such a number lands on a member or in a hole between them is luck
        /// that changes every time a member is appended, so the refusal belongs here.
        /// <para>A flag enum is the one place a list of names and a raw mask are what they look like, and
        /// it keeps both.</para>
        /// </summary>
        private static bool NamesOneMember<TEnum>(string value)
            where TEnum : struct, Enum =>
            typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false)
            || (!value.Contains(',') && !long.TryParse(value, out _));
    }
}
