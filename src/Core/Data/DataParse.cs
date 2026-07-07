namespace Core.Data
{
    using System;

    /// <summary>
    /// The one enum-parsing policy for game data: strict and case-insensitive. A typo in JSON
    /// must fail the file loudly, never silently fall back to the first enum member.
    /// </summary>
    public static class DataParse
    {
        public static TEnum ParseEnum<TEnum>(string value)
            where TEnum : struct, Enum =>
            Enum.TryParse(value, ignoreCase: true, out TEnum result)
                ? result
                : throw new FormatException($"'{value}' is not a valid {typeof(TEnum).Name}");

        /// <summary>For genuinely optional fields: absent value means default, a present value is parsed strictly.</summary>
        public static TEnum ParseEnumOrDefault<TEnum>(string? value)
            where TEnum : struct, Enum =>
            string.IsNullOrEmpty(value) ? default : ParseEnum<TEnum>(value);
    }
}
