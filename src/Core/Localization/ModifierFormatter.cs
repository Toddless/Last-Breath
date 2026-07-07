namespace Core.Localization
{
    using System;
    using System.Globalization;
    using Enums;
    using Modifiers;

    /// <summary>
    /// Turns a modifier into a localized phrase through .po templates:
    /// Modifier_Flat / Modifier_Increase / Modifier_Multiplicative with {value} and {parameter}.
    /// Percent parameters follow IParameterFormatProvider (data stores fractions: 0.5 = 50%).
    /// </summary>
    public class ModifierFormatter(ILocalizationProvider localization, IParameterFormatProvider formats)
    {
        public string Format(IModifier modifier, TextFormat format = TextFormat.Plain) => modifier.ModifierValueType switch
        {
            ModifierValueType.Flat => RenderFlat(modifier, 1f, 1f, format),
            ModifierValueType.Increase => RenderIncrease(modifier, 1f, 1f, format),
            ModifierValueType.Multiplicative => RenderMultiplicative(modifier, 1f, 1f, format),
            _ => RenderFallback(modifier, format)
        };

        /// <summary>Crafting preview: the value rolls within [min..max] multipliers of the base.</summary>
        public string FormatRanged(IModifier modifier, float rangeMinValue, float rangeMaxValue, TextFormat format = TextFormat.Plain) => modifier.ModifierValueType switch
        {
            ModifierValueType.Flat => RenderFlat(modifier, rangeMinValue, rangeMaxValue, format),
            ModifierValueType.Increase => RenderIncrease(modifier, rangeMinValue, rangeMaxValue, format),
            ModifierValueType.Multiplicative => RenderMultiplicative(modifier, rangeMinValue, rangeMaxValue, format),
            _ => RenderFallback(modifier, format)
        };

        private string RenderFlat(IModifier modifier, float rangeMin, float rangeMax, TextFormat format)
        {
            bool isPercent = formats.GetUnit(modifier.EntityParameter) == ParameterUnit.Percent;
            float scale = isPercent ? 100f : 1f;
            string value = SignedRange(modifier.Value * rangeMin * scale, modifier.Value * rangeMax * scale, isPercent);
            return Render("Modifier_Flat", value, modifier.EntityParameter, format);
        }

        private string RenderIncrease(IModifier modifier, float rangeMin, float rangeMax, TextFormat format)
        {
            string value = SignedRange(modifier.Value * rangeMin * 100f, modifier.Value * rangeMax * 100f, isPercent: true);
            return Render("Modifier_Increase", value, modifier.EntityParameter, format);
        }

        private string RenderMultiplicative(IModifier modifier, float rangeMin, float rangeMax, TextFormat format)
        {
            float deltaMin = (modifier.Value * rangeMin - 1f) * 100f;
            float deltaMax = (modifier.Value * rangeMax - 1f) * 100f;
            string value = SignedRange(deltaMin, deltaMax, isPercent: true);
            return Render("Modifier_Multiplicative", value, modifier.EntityParameter, format);
        }

        /// <summary>
        /// One ParameterChange as a display value, unit-aware: Add CriticalChance 0.05 → "+5%",
        /// Multiply 1.2 → "+20%", Override → the absolute value without a sign.
        /// </summary>
        public string FormatParameterChange(EntityParameter parameter, float value, OperationType operation, TextFormat format = TextFormat.Plain)
        {
            bool isPercent = formats.GetUnit(parameter) == ParameterUnit.Percent;
            float scale = isPercent ? 100f : 1f;
            string text = operation switch
            {
                OperationType.Add => SignedRange(value * scale, value * scale, isPercent),
                OperationType.Subtract => SignedRange(-value * scale, -value * scale, isPercent),
                OperationType.Multiply => SignedRange((value - 1f) * 100f, (value - 1f) * 100f, isPercent: true),
                OperationType.Divide => SignedRange((1f / value - 1f) * 100f, (1f / value - 1f) * 100f, isPercent: true),
                _ => Number(value * scale) + (isPercent ? "%" : string.Empty)
            };
            return format == TextFormat.Rich ? TextPalette.ColorizeNumber(text) : text;
        }

        /// <summary>Uninitialized modifier type in legacy data: fractions read as increase, the rest as flat.</summary>
        private string RenderFallback(IModifier modifier, TextFormat format) =>
            MathF.Abs(modifier.Value) is > 0f and < 1f
                ? RenderIncrease(modifier, 1f, 1f, format)
                : RenderFlat(modifier, 1f, 1f, format);

        private string Render(string templateKey, string value, EntityParameter parameter, TextFormat format)
        {
            string styledValue = format == TextFormat.Rich ? TextPalette.ColorizeNumber(value) : value;
            return localization.Translate(templateKey)
                .Replace("{value}", styledValue)
                .Replace("{parameter}", localization.Translate(parameter.ToString()));
        }

        private static string SignedRange(float min, float max, bool isPercent)
        {
            string sign = min >= 0 ? "+" : "-";
            string suffix = isPercent ? "%" : string.Empty;
            string minText = Number(MathF.Abs(min));
            if (MathF.Abs(max - min) < 0.0001f) return $"{sign}{minText}{suffix}";
            return $"{sign}{minText}-{Number(MathF.Abs(max))}{suffix}";
        }

        private static string Number(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
