namespace Core.Localization
{
    using System;
    using System.Globalization;
    using Enums;
    using Modifiers;

    /// <summary>
    /// Turns a modifier into a localized phrase through .po templates:
    /// Modifier_Flat / Modifier_Increase / Modifier_Multiplicative with {value} and {parameter};
    /// unrolled value spreads render through the *_Range twins with {min} and {max}
    /// ("+40–60 Strength"), and a percent penalty through the *_Negative twins, which word the
    /// minus instead of printing it ("25% less Health Recovery"). Percent parameters follow
    /// IParameterFormatProvider (data stores fractions: 0.5 = 50%).
    /// </summary>
    public class ModifierFormatter(ILocalizationProvider localization, IParameterFormatProvider formats)
    {
        public string Format(IModifier modifier, TextFormat format = TextFormat.Plain) =>
            RenderLine(modifier.ModifierValueType, modifier.EntityParameter, modifier.Value, modifier.Value, format);

        /// <summary>Crafting preview: the value rolls within [min..max] multipliers of the base.</summary>
        public string FormatRanged(IModifier modifier, float rangeMinValue, float rangeMaxValue, TextFormat format = TextFormat.Plain) =>
            RenderLine(modifier.ModifierValueType, modifier.EntityParameter, modifier.Value * rangeMinValue, modifier.Value * rangeMaxValue, format);

        /// <summary>An unmaterialized pool/blueprint line: a fixed value renders like a live modifier,
        /// a spread renders through the range templates.</summary>
        public string FormatDescriptor(ParameterDescriptor descriptor, TextFormat format = TextFormat.Plain) =>
            RenderLine(descriptor.ValueType, descriptor.Parameter, descriptor.Value.Min, descriptor.Value.Max, format);

        /// <summary>The roll spread of a materialized line as a bare interval ("40–60", "4–8%") for the
        /// Alt reveal. Bounds are scaled to the line's current value channel (Value/BaseValue carries the
        /// item's update multiplier), so they stay comparable with the shown number. Null when the line
        /// was minted from a fixed value.</summary>
        public string? FormatRolledRange(IModifier modifier, TextFormat format = TextFormat.Plain)
        {
            if (modifier is not SimpleModifier { RolledRange: { } range } simple) return null;

            float levelScale = Math.Abs(simple.BaseValue) < 0.0001f ? 1f : simple.Value / simple.BaseValue;
            bool isPercent = IsPercentDisplay(modifier.ModifierValueType, modifier.EntityParameter);
            float unitScale = (isPercent ? 100f : 1f) * levelScale;
            string text = $"{Number(range.Min * unitScale)}–{Number(range.Max * unitScale)}{(isPercent ? "%" : string.Empty)}";
            return format == TextFormat.Rich ? TextPalette.ColorizeNumber(text) : text;
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
                OperationType.Add => SignedValue(value * scale, isPercent),
                OperationType.Subtract => SignedValue(-value * scale, isPercent),
                OperationType.Multiply => SignedValue((value - 1f) * 100f, isPercent: true),
                OperationType.Divide => SignedValue((1f / value - 1f) * 100f, isPercent: true),
                _ => Number(value * scale) + (isPercent ? "%" : string.Empty)
            };
            return format == TextFormat.Rich ? TextPalette.ColorizeNumber(text) : text;
        }

        /// <summary>A bare display value ("45.7%", "130.8") for tabular previews — the same unit
        /// logic the line templates use, without sign or template wording.</summary>
        public string FormatValue(ModifierValueType valueType, EntityParameter parameter, float value)
        {
            bool isPercent = IsPercentDisplay(valueType, parameter);
            return Number(value * (isPercent ? 100f : 1f)) + (isPercent ? "%" : string.Empty);
        }

        /// <summary>A descriptor's roll bounds as a bare interval ("98.1 – 228.9", "5.5 – 16.4%")
        /// for pool tables; a fixed value renders as the single number.</summary>
        public string FormatDescriptorRange(ParameterDescriptor descriptor)
        {
            bool isPercent = IsPercentDisplay(descriptor.ValueType, descriptor.Parameter);
            float scale = isPercent ? 100f : 1f;
            string unit = isPercent ? "%" : string.Empty;
            return descriptor.Value.IsFixed
                ? Number(descriptor.Value.Min * scale) + unit
                : $"{Number(descriptor.Value.Min * scale)} – {Number(descriptor.Value.Max * scale)}{unit}";
        }

        private string RenderLine(ModifierValueType valueType, EntityParameter parameter, float min, float max, TextFormat format)
        {
            // Multiplicative modifier Value is a DELTA, not a full multiplier: the engine folds it as
            // (1 + Σ Value) (see Calculations.CalculateModifiers), so 0.2 means "+20% more". Rendering
            // it as (Value - 1) turned every "more" line negative (-80% for a +20% mod).
            string templateKey = valueType switch
            {
                ModifierValueType.Flat => "Modifier_Flat",
                ModifierValueType.Increase => "Modifier_Increase",
                ModifierValueType.Multiplicative => "Modifier_Multiplicative",
                // Uninitialized modifier type in legacy data: fractions read as increase, the rest as flat.
                _ => MathF.Abs(min) is > 0f and < 1f ? "Modifier_Increase" : "Modifier_Flat",
            };
            bool isPercent = templateKey != "Modifier_Flat" || formats.GetUnit(parameter) == ParameterUnit.Percent;

            // The one place a sign decides wording: a percent penalty reads "25% less" rather than
            // "-25% more", so the template carries the minus and the number sheds it — the bounds
            // flip to stay ascending. Flat keeps its sign, "-25 Health" already reads as a loss.
            bool penalty = templateKey != "Modifier_Flat" && min < 0f && max < 0f;
            if (penalty)
            {
                templateKey += "_Negative";
                (min, max) = (-max, -min);
            }

            float scale = isPercent ? 100f : 1f;
            return MathF.Abs(max - min) < 0.0001f
                ? Render(templateKey, Value(min * scale, isPercent, signed: !penalty), parameter, format)
                : RenderRange(templateKey + "_Range", min * scale, max * scale, isPercent, parameter, format, signed: !penalty);
        }

        /// <summary>Range twin of Render: {min} carries the sign unless the wording already does,
        /// {max} carries the unit ("+40–60% ...").</summary>
        private string RenderRange(
            string templateKey, float min, float max, bool isPercent, EntityParameter parameter, TextFormat format, bool signed = true)
        {
            string minText = $"{Sign(min, signed)}{Number(MathF.Abs(min))}";
            string maxText = $"{Number(MathF.Abs(max))}{(isPercent ? "%" : string.Empty)}";
            if (format == TextFormat.Rich)
            {
                minText = TextPalette.ColorizeNumber(minText);
                maxText = TextPalette.ColorizeNumber(maxText);
            }

            return localization.Translate(templateKey)
                .Replace("{min}", minText)
                .Replace("{max}", maxText)
                .Replace("{parameter}", localization.Translate(parameter.ToString()));
        }

        private string Render(string templateKey, string value, EntityParameter parameter, TextFormat format)
        {
            string styledValue = format == TextFormat.Rich ? TextPalette.ColorizeNumber(value) : value;
            return localization.Translate(templateKey)
                .Replace("{value}", styledValue)
                .Replace("{parameter}", localization.Translate(parameter.ToString()));
        }

        private bool IsPercentDisplay(ModifierValueType valueType, EntityParameter parameter) =>
            valueType != ModifierValueType.Flat || formats.GetUnit(parameter) == ParameterUnit.Percent;

        private static string SignedValue(float value, bool isPercent) => Value(value, isPercent, signed: true);

        /// <summary>The number as the templates take it: unit-suffixed, and signed unless the wording
        /// already says which way it goes.</summary>
        private static string Value(float value, bool isPercent, bool signed) =>
            $"{Sign(value, signed)}{Number(MathF.Abs(value))}{(isPercent ? "%" : string.Empty)}";

        private static string Sign(float value, bool signed) => !signed ? string.Empty : value >= 0 ? "+" : "-";

        private static string Number(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
