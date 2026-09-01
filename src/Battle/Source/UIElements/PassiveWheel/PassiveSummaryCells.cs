namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Localization;
    using Core.PassiveTree.Summary;

    /// <summary>
    /// The strings a totals row prints, worked out away from the scene: which bucket cell a line lands in,
    /// and what a planned allocation would add to it.
    /// </summary>
    public static class PassiveSummaryCells
    {
        /// <summary>What marks the "more" part of a delta, where no heading is there to name the bucket.</summary>
        private const string MoreMark = "×";

        private const float Tolerance = 0.0001f;

        /// <summary>The three bucket cells of one parameter line. A bucket that summed to nothing prints
        /// nothing — a table of zeroes is harder to read than a short one — but its cell stays in the
        /// row.</summary>
        public static (string Flat, string Increase, string More) Buckets(ModifierFormatter? formatter, ParameterTotal total) => (
            Bucket(formatter, total.Parameter, ModifierValueType.Flat, total.Flat),
            Bucket(formatter, total.Parameter, ModifierValueType.Increase, total.Increase),
            Bucket(formatter, total.Parameter, ModifierValueType.Multiplicative, total.Multiplicative));

        /// <summary>What the plan would ADD to a line, bucket by bucket ("+20, +10%, ×5%"). Summing the
        /// three into one number would need a fighter to fold them against, and the panel has none — so
        /// each bucket is quoted in its own units and the parts that changed by nothing are left out.</summary>
        public static string PlanDelta(ModifierFormatter? formatter, ParameterTotal held, ParameterTotal? projected)
        {
            List<string> parts = [];
            Part(parts, formatter, held.Parameter, ModifierValueType.Flat, (projected?.Flat ?? 0f) - held.Flat);
            Part(parts, formatter, held.Parameter, ModifierValueType.Increase, (projected?.Increase ?? 0f) - held.Increase);
            Part(parts, formatter, held.Parameter, ModifierValueType.Multiplicative,
                (projected?.Multiplicative ?? 0f) - held.Multiplicative);

            return string.Join(", ", parts);
        }

        private static void Part(
            List<string> parts, ModifierFormatter? formatter, EntityParameter parameter, ModifierValueType valueType, float delta)
        {
            if (MathF.Abs(delta) < Tolerance) return;

            string sign = delta < 0f ? "-" : valueType == ModifierValueType.Multiplicative ? string.Empty : "+";
            string mark = valueType == ModifierValueType.Multiplicative ? MoreMark : string.Empty;
            parts.Add(mark + sign + Value(formatter, parameter, valueType, MathF.Abs(delta)));
        }

        private static string Bucket(ModifierFormatter? formatter, EntityParameter parameter, ModifierValueType valueType, float value) =>
            MathF.Abs(value) < Tolerance ? string.Empty : Value(formatter, parameter, valueType, value);

        /// <summary>The number in the units the rest of the game states this parameter in; a composition
        /// without a formatter prints the raw part rather than taking the panel down.</summary>
        private static string Value(ModifierFormatter? formatter, EntityParameter parameter, ModifierValueType valueType, float value) =>
            formatter?.FormatValue(valueType, parameter, value) ?? $"{value:0.#}";
    }
}
