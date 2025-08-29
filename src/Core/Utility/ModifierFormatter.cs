namespace Core.Utility
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Core.Modifiers;

    public class ModifierFormatter
    {
        private readonly Func<string, string> _localize;

        public ModifierFormatter(Func<string, string> localize)
        {
            _localize = localize;
        }

        public string FormatMaterialModifier(IMaterialModifier modifier)
        {
            return string.Format(GetTemplate(), _localize.Invoke($"{modifier.Parameter}"), GetValueStringModifierType(modifier.ModifierType, modifier.Value));
        }

        public string FormatModifier(IModifier modifier)
        {
            return string.Format(GetTemplate(), _localize.Invoke($"{modifier.Parameter}"), GetValueStringModifierType(modifier.Type, modifier.Value));
        }

        public string FormatItemStats(string propertyName, float value)
        {
            return string.Format(GetTemplate(), _localize.Invoke(propertyName), GetValueString(value));
        }

        private string GetTemplate() => _localize.Invoke("modifier.format");

        private string GetValueString(float value)
        {
            // TODO: Rework this later, if multiplier can be bigger than x4
            return true switch
            {
                bool _ when value > 0 && value < 1 => FormatPercent(value),
                bool _ when value > 1 && value < 4 => FormatMultiplier(value),
                bool _ when value > 5 => FormatFlat(value),
                _ => FallbackFormat(value)
            };
        }

        private string GetValueStringModifierType(ModifierType type, float value)
        {
            return type switch
            {
                ModifierType.Flat => FormatFlat(value),
                ModifierType.Increase => FormatPercent(value),
                ModifierType.Multiplicative => FormatMultiplier(value),
                _ => FallbackFormat(value)
            };
        }

        private string FormatFlat(float value) => string.Format("{0}{1:0.##}", value >= 0 ? "+" : "", MathF.Abs(value));
        private string FormatPercent(float value)
        {
            float percent = value * 100;
            return string.Format("{0}{1:0.##}%", percent >= 0 ? "+" : "", percent);
        }
        private string FormatMultiplier(float value) => string.Format("x{0:0.##}", value);
        private string FallbackFormat(float value)
        {
            if (MathF.Abs(value) > 0 && MathF.Abs(value) < 1) return FormatPercent(value);
            return FormatFlat(value);
        }
    }
}
