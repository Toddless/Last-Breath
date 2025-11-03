namespace Utilities
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Godot;

    public class ModifierFormatter(Func<string, string> localize)
    {
        // _localize here is public static string GetLokalizedName(string id) => TranslationServer.Translate(id); from lokalizator
        private readonly Func<string, string> _localize = localize;

        public string FormatModifier(IModifier modifier)
        {
            ArgumentNullException.ThrowIfNull(modifier);

            return modifier.ModifierType switch
            {
                ModifierType.Flat => FormatFlat(modifier.Value, modifier.Parameter),
                ModifierType.Increase => FormatIncrease(modifier.Value, modifier.Parameter),
                ModifierType.Multiplicative => FormatMultiplicative(modifier.Value, modifier.Parameter),
                _ => FormatFallback(modifier.Value, modifier.Parameter)
            };
        }

        private string FormatFlat(float value, Parameter parameter)
        {
            string number = FormatSignedNumber(value);
            string percentSign = GetPercentSigh(parameter);
            return $"{number + percentSign} {_localize.Invoke("Flat")} {_localize.Invoke(parameter.ToString())}";
        }

        private string GetPercentSigh(Parameter parameter)
        {
            return parameter switch
            {
                Parameter.CriticalChance => "%",
                Parameter.CriticalDamage => "%",
                Parameter.AdditionalHitChance => "%",
                Parameter.MaxEvadeChance => "%",
                Parameter.MaxCriticalChance => "%",
                Parameter.MaxAdditionalHitChance => "%",
                _ => string.Empty,
            };
        }

        private string FormatIncrease(float value, Parameter parameter)
        {
            string percent = FormatSignedPercent(value * 100f);
            return $"{percent} {_localize.Invoke("Increase")} {_localize.Invoke(parameter.ToString())}";
        }

        private string FormatMultiplicative(float value, Parameter parameter)
        {
            float deltaPercent = (value - 1f) * 100f;
            string percent = FormatSignedPercent(deltaPercent);
            return $"{percent} {_localize.Invoke("Multiplicative")} {_localize.Invoke(parameter.ToString())}";
        }

        private string FormatFallback(float value, Parameter parameter)
        {
            float abs = MathF.Abs(value);
            if (abs > 0f && abs < 1f)
                return FormatIncrease(value, parameter);
            return FormatFlat(value, parameter);
        }

        private string FormatSignedNumber(float value)
        {
            string sign = value >= 0 ? "+" : "-";
            float abs = MathF.Abs(value);
            return $"{sign}{(abs % 1 == 0 ? Mathf.RoundToInt(abs) : abs.ToString("0.#"))}";
        }

        private string FormatSignedPercent(float percent)
        {
            string sign = percent >= 0 ? "+" : "-";
            float abs = MathF.Abs(percent);
            return $"{sign}{abs:0.#}%";
        }
    }
}
