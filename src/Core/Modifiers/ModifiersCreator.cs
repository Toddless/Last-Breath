namespace Core.Modifiers
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using System.Collections.Generic;

    public class ModifiersCreator
    {
        private static readonly Dictionary<string, Parameter> s_parameterMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Damage"] = Parameter.Damage,
            ["AllAttribute"] = Parameter.AllAttribute,
            ["Intelligence"] = Parameter.Intelligence,
            ["Dexterity"] = Parameter.Dexterity,
            ["Strength"] = Parameter.Strength,
            ["CriticalChance"] = Parameter.CriticalChance,
            ["CriticalDamage"] = Parameter.CriticalDamage,
            ["AdditionalHitChance"] = Parameter.AdditionalHitChance,
            ["Armor"] = Parameter.Armor,
            ["Evade"] = Parameter.Evade,
            ["EnergyBarrier"] = Parameter.EnergyBarrier,
            ["SpellDamage"] = Parameter.SpellDamage,
            ["ResourceRecovery"] = Parameter.ResourceRecovery,
            ["Movespeed"] = Parameter.Movespeed,
            ["Suppress"] = Parameter.Suppress,
            ["Health"] = Parameter.MaxHealth,
            ["MaxResource"] = Parameter.MaxResource,
            ["MaxEvadeChance"] = Parameter.MaxEvadeChance,
            ["MaxReduceDamage"] = Parameter.MaxReduceDamage,
        };

        private static readonly Dictionary<string, ModifierType> s_typeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["flat"] = ModifierType.Flat,
            ["increase"] = ModifierType.Increase,
            ["inc"] = ModifierType.Increase,
            ["mult"] = ModifierType.Multiplicative,
            ["multiplicative"] = ModifierType.Multiplicative,
            ["mul"] = ModifierType.Multiplicative
        };

        public static List<IModifierInstance> CreateModifierInstances(List<IModifier> stats, object source)
        {
            List<IModifierInstance> modifiers = [];
            stats.ForEach(mod => modifiers.Add(CreateModifierInstance(mod.Parameter, mod.ModifierType, mod.BaseValue, source)));
            return modifiers;
        }

        public static List<IModifier> ConvertDtoToModifiers(List<ModifierDto> dtos)
        {
            var modifiers = new List<IModifier>();

            foreach (var d in dtos)
            {
                if (!s_parameterMapping.TryGetValue(d.Parameter, out var param)) continue;
                if (!s_typeMap.TryGetValue(d.Type ?? "flat", out var type)) continue;
                if (d.Value <= 0) continue;

                var modifier = CreateModifier(param, type, d.Value);
                modifiers.Add(modifier);
            }

            return modifiers;
        }

        public static IModifier CreateModifier(Parameter parameter, ModifierType type, float baseValue)
            => new Modifier(type, parameter, baseValue);

        public static IModifierInstance CreateModifierInstance(Parameter parameter, ModifierType modifierType, float value, object source, int priority = 10)
            => new ModifierInstance(parameter, modifierType, value, source, priority);
    }
}
