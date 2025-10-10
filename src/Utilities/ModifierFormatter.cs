namespace Utilities
{
    using System;
    using Core.Enums;
    using Core.Interfaces;

    public class ModifierFormatter(Func<string, string> localize)
    {
        // _localize here is public static string GetLokalizedName(string id) => TranslationServer.Translate(id); from lokalizator
        private readonly Func<string, string> _localize = localize;

        public string FormatModifier(IModifier modifier)
        {
            return string.Format(GetTemplate(), _localize.Invoke(CombineParameterWithType(modifier.ModifierType, modifier.Parameter)), GetValueStringModifierType(modifier.ModifierType, modifier.Value));
        }

        public string FormatItemStats(string propertyName, float value)
        {
            return string.Format(GetTemplate(), _localize.Invoke(propertyName), GetValueString(value));
        }

        private string GetTemplate() => _localize.Invoke("modifier.format");

        private string GetValueString(float value)
        {
            // Rework this later, if multiplier will be bigger than x4
            return true switch
            {
                bool _ when value < 1 => FormatPercent(value),
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
        private string FormatMultiplier(float value) => string.Format("x{0:0.##}", value);
        private string FormatPercent(float value)
        {
            float percent = value * 100;
            return string.Format("{0}{1:0.##}%", percent >= 0 ? "+" : "", percent);
        }
        private string FallbackFormat(float value)
        {
            if (MathF.Abs(value) > 0 && MathF.Abs(value) < 1) return FormatPercent(value);
            return FormatFlat(value);
        }

        private string CombineParameterWithType(ModifierType type, Parameter parameter)
        {
            // TODO: should multiplicative be called "Extra"?
            return type switch
            {
                ModifierType.Multiplicative => parameter switch
                {
                    Parameter.Damage => $"Extra{parameter}",
                    Parameter.Armor => $"Extra{parameter}",
                    Parameter.Evade => $"Extra{parameter}",
                    Parameter.EnergyBarrier => $"Extra{parameter}",
                    Parameter.Suppress => $"Extra{parameter}",
                    Parameter.SpellDamage => $"Extra{parameter}",
                    Parameter.MaxHealth => "ExtraHealth",
                    _ => $"{parameter}"
                },
                _ => $"{parameter}"
            };
        }
    }
}
