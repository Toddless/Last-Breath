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
                ModifierType.Flat => FormatFlat(modifier.Value, modifier.EntityParameter),
                ModifierType.Increase => FormatIncrease(modifier.Value, modifier.EntityParameter),
                ModifierType.Multiplicative => FormatMultiplicative(modifier.Value, modifier.EntityParameter),
                _ => FormatFallback(modifier.Value, modifier.EntityParameter)
            };
        }

        private string FormatFlat(float value, EntityParameter entityParameter)
        {
            string number = FormatSignedNumber(value);
            string percentSign = GetPercentSigh(entityParameter);
            return $"{number + percentSign} {_localize.Invoke("Flat")} {_localize.Invoke(entityParameter.ToString())}";
        }

        private string GetPercentSigh(EntityParameter entityParameter)
        {
            return entityParameter switch
            {
                EntityParameter.CriticalChance => "%",
                EntityParameter.CriticalDamage => "%",
                EntityParameter.AdditionalHitChance => "%",
                EntityParameter.MaxEvadeChance => "%",
                _ => string.Empty,
            };
        }

        private string FormatIncrease(float value, EntityParameter entityParameter)
        {
            string percent = FormatSignedPercent(value * 100f);
            return $"{percent} {_localize.Invoke("Increase")} {_localize.Invoke(entityParameter.ToString())}";
        }

        private string FormatMultiplicative(float value, EntityParameter entityParameter)
        {
            float deltaPercent = (value - 1f) * 100f;
            string percent = FormatSignedPercent(deltaPercent);
            return $"{percent} {_localize.Invoke("Multiplicative")} {_localize.Invoke(entityParameter.ToString())}";
        }

        private string FormatFallback(float value, EntityParameter entityParameter)
        {
            float abs = MathF.Abs(value);
            if (abs > 0f && abs < 1f)
                return FormatIncrease(value, entityParameter);
            return FormatFlat(value, entityParameter);
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
