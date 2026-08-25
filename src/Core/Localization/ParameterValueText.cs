namespace Core.Localization
{
    using System.Globalization;
    using Entity.Components;
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

        /// <summary>A resistance reads as what actually mitigates, with the uncapped total in brackets —
        /// shown only while the entity stands above its maximum, where the surplus is a reserve against shred.</summary>
        public static string FormatResistance(IParameterFormatProvider? formats, EntityParameter resistance, float total, float maximum)
        {
            float effective = ResistanceParameters.Effective(total, maximum);
            string shown = Format(formats, resistance, effective);
            return total > effective ? $"{shown} ({Format(formats, resistance, total)})" : shown;
        }

        /// <summary>Display of a parameter read off an entity: a resistance shows its cap, anything else its value.</summary>
        public static string Format(IParameterFormatProvider? formats, EntityParameter parameter, IEntityParametersComponent parameters) =>
            ResistanceParameters.MaximumFor(parameter) is { } maximum
                ? FormatResistance(formats, parameter, parameters.GetValueForParameter(parameter), parameters.GetValueForParameter(maximum))
                : Format(formats, parameter, parameters.GetValueForParameter(parameter));
    }
}
