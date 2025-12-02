namespace Core.Modifiers
{
    using System;
    using Enums;
    using Interfaces;
    using Interfaces.Data;
    using System.Collections.Generic;

    public class ModifiersCreator
    {
        private static readonly Dictionary<string, EntityParameter> s_parameterMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Damage"] = EntityParameter.Damage,
            ["Intelligence"] = EntityParameter.Intelligence,
            ["Dexterity"] = EntityParameter.Dexterity,
            ["Strength"] = EntityParameter.Strength,
            ["CriticalChance"] = EntityParameter.CriticalChance,
            ["CriticalDamage"] = EntityParameter.CriticalDamage,
            ["AdditionalHitChance"] = EntityParameter.AdditionalHitChance,
            ["Armor"] = EntityParameter.Armor,
            ["Evade"] = EntityParameter.Evade,
            ["EnergyBarrier"] = EntityParameter.Barrier,
            ["SpellDamage"] = EntityParameter.SpellDamage,
            ["ResourceRecovery"] = EntityParameter.ManaRecovery,
            ["Movespeed"] = EntityParameter.MoveSpeed,
            ["Suppress"] = EntityParameter.Suppress,
            ["Health"] = EntityParameter.Health,
            ["MaxResource"] = EntityParameter.Mana,
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
            stats.ForEach(mod => modifiers.Add(CreateModifierInstance(mod.EntityParameter, mod.ModifierType, mod.BaseValue, source)));
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

        public static IModifier CreateModifier(EntityParameter entityParameter, ModifierType type, float baseValue)
            => new Modifier(type, entityParameter, baseValue);

        public static IModifierInstance CreateModifierInstance(EntityParameter entityParameter, ModifierType modifierType, float value, object source, int priority = 10)
            => new ModifierInstance(entityParameter, modifierType, value, source, priority);
    }
}
