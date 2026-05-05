namespace Utilities
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Modifiers;
    using Godot;

    public class ModifierFormatter(Func<string, string> localize)
    {
        private static readonly List<EntityParameter> s_percentParameters =
        [
            EntityParameter.CriticalChance,
            EntityParameter.CriticalDamage,
            EntityParameter.AdditionalHitChance,
            EntityParameter.ArmorPenetration,
            EntityParameter.BlockChance,
            EntityParameter.MulticastChance
        ];

        public string FormatModifier(IModifier modifier, float rangeMinValue, float rangeMaxValue) => modifier.ModifierValueType switch
        {
            ModifierValueType.Flat => FormatFlatRanged(modifier.Value, modifier.EntityParameter, rangeMinValue, rangeMaxValue),
            ModifierValueType.Increase => FormatIncreaseRanged(modifier.Value, modifier.EntityParameter, rangeMinValue, rangeMaxValue),
            ModifierValueType.Multiplicative => FormatMultiplicativeRanged(modifier.Value, modifier.EntityParameter, rangeMinValue, rangeMaxValue),
            _ => string.Empty
        };

        public string FormatModifier(IModifier modifier)
        {
            return modifier.ModifierValueType switch
            {
                ModifierValueType.Flat => FormatFlat(modifier.Value, modifier.EntityParameter),
                ModifierValueType.Increase => FormatIncrease(modifier.Value, modifier.EntityParameter),
                ModifierValueType.Multiplicative => FormatMultiplicative(modifier.Value, modifier.EntityParameter),
                _ => FormatFallback(modifier.Value, modifier.EntityParameter)
            };
        }

        private string FormatFlat(float value, EntityParameter entityParameter)
        {
            string number = FormatSignedNumber(value);
            string percentSign = GetPercentSigh(entityParameter);
            return $"{number + percentSign} {localize.Invoke("Flat")} {localize.Invoke(entityParameter.ToString())}";
        }

        private string FormatIncrease(float value, EntityParameter entityParameter)
        {
            string percent = FormatSignedPercent(value * 100f);
            return $"{percent} {localize.Invoke("Increase")} {localize.Invoke(entityParameter.ToString())}";
        }

        private string FormatMultiplicative(float value, EntityParameter entityParameter)
        {
            float deltaPercent = (value - 1f) * 100f;
            string percent = FormatSignedPercent(deltaPercent);
            return $"{percent} {localize.Invoke("Multiplicative")} {localize.Invoke(entityParameter.ToString())}";
        }

        private string FormatFlatRanged(float modifierValue, EntityParameter entityParameter, float rangeMinValue, float rangeMaxValue)
        {
            string formatedNumber = FormatSignedNumbersRanged(modifierValue * rangeMinValue, modifierValue * rangeMaxValue);
            string percentSign = GetPercentSigh(entityParameter);
            return $"{formatedNumber + percentSign} {localize.Invoke("Flat")} {localize.Invoke(entityParameter.ToString())}";
        }

        private string FormatIncreaseRanged(float modifierValue, EntityParameter entityParameter, float rangeMinValue, float rangeMaxValue)
        {
            string formatedNumber = FormatSignedPercentRanged((modifierValue * rangeMinValue) * 100, (modifierValue * rangeMaxValue) * 100);
            return $"{formatedNumber} {localize.Invoke("Increase")} {localize.Invoke(entityParameter.ToString())}";
        }


        private string FormatMultiplicativeRanged(float modifierValue, EntityParameter entityParameter, float rangeMinValue, float rangeMaxValue)
        {
            float deltaMinPercent = (modifierValue * rangeMinValue - 1f) * 100f;
            float deltaMaxPercent = (modifierValue * rangeMaxValue - 1f) * 100f;
            string formatedNumber = FormatSignedPercentRanged(deltaMinPercent, deltaMaxPercent);
            return $"{formatedNumber} {localize.Invoke("Multiplicative")} {localize.Invoke(entityParameter.ToString())}";
        }


        private string FormatFallback(float value, EntityParameter entityParameter)
        {
            float abs = MathF.Abs(value);
            return abs is > 0f and < 1f ? FormatIncrease(value, entityParameter) : FormatFlat(value, entityParameter);
        }

        private string FormatSignedNumber(float value)
        {
            string sign = value >= 0 ? "+" : "-";
            float abs = MathF.Abs(value);
            return $"{sign}{(abs % 1 < 0.0001f ? Mathf.RoundToInt(abs) : abs.ToString("0.#"))}";
        }

        private string FormatSignedPercent(float percent)
        {
            string sign = percent >= 0 ? "+" : "-";
            float abs = MathF.Abs(percent);
            return $"{sign}{abs:0.#}%";
        }

        private string FormatSignedNumbersRanged(float minValue, float maxValue)
        {
            string sign = minValue >= 0 ? "+" : "-";
            float absMin = Mathf.Abs(minValue);
            float absMax = Mathf.Abs(maxValue);
            return
                $"{sign}{(absMin % 1 < 0.0001f ? Mathf.RoundToInt(absMin) : absMin.ToString("0.#"))} - {(absMax % 1 < 0.0001f ? Mathf.RoundToInt(absMax) : absMax.ToString("0.#"))}";
        }

        private string FormatSignedPercentRanged(float minValue, float maxValue)
        {
            string sign = minValue >= 0 ? "+" : "-";
            float absMin = Mathf.Abs(minValue);
            float absMax = Mathf.Abs(maxValue);
            return $"{sign}{absMin:0.#}-{absMax:0.#}%";
        }

        private string GetPercentSigh(EntityParameter entityParameter) => s_percentParameters.Contains(entityParameter) ? "%" : string.Empty;
    }
}
