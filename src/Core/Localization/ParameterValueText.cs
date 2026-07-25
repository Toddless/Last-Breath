namespace Core.Localization
{
    using System.Globalization;
    using Enums;

    /// <summary>
    /// Plain display of a parameter VALUE (character sheet, item stats): ParameterFormats.json
    /// decides which parameters are fractions-as-percent. Modifier LINES (+X / +X%) are
    /// <see cref="ModifierFormatter"/>'s job — this is only for bare current values.
    /// </summary>
    public static class ParameterValueText
    {
        public static string Format(IParameterFormatProvider? formats, EntityParameter parameter, float value) =>
            formats?.GetUnit(parameter) == ParameterUnit.Percent
                ? $"{(value * 100).ToString("0.#", CultureInfo.InvariantCulture)}%"
                : value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
