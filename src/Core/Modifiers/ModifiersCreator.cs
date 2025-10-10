namespace Core.Modifiers
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using System.Collections.Generic;

    public class ModifiersCreator
    {
        private record ModifierMapping(Parameter Parameter, ModifierType ModifierType, int Prioritry = 10);

        private static readonly Dictionary<string, ModifierMapping> s_parameterMapping = new()
        {
            ["MaxReduceDamage"] = new(Parameter.MaxReduceDamage, ModifierType.Flat),
            ["MaxEvadeChance"] = new(Parameter.MaxEvadeChance, ModifierType.Flat),
            ["AdditionalHitChance"] = new(Parameter.AdditionalHitChance, ModifierType.Flat),
            ["Suppress"] = new(Parameter.Suppress, ModifierType.Flat),
            ["Resource"] = new(Parameter.ResourceMax, ModifierType.Flat),
            ["ResourceRecovery"] = new(Parameter.ResourceRecovery, ModifierType.Flat),
            ["CriticalDamage"] = new(Parameter.CriticalDamage, ModifierType.Flat),
            ["Dexterity"] = new(Parameter.Dexterity, ModifierType.Flat),
            ["Strength"] = new(Parameter.Strength, ModifierType.Flat),
            ["Intelligence"] = new(Parameter.Intelligence, ModifierType.Flat),
            ["AllAttribute"] = new(Parameter.AllAttribute, ModifierType.Flat),
            ["Movespeed"] = new(Parameter.Movespeed, ModifierType.Flat),
            ["Armor"] = new(Parameter.Armor, ModifierType.Flat),
            ["EnergyBarrier"] = new(Parameter.EnergyBarrier, ModifierType.Flat),
            ["SpellDamage"] = new(Parameter.SpellDamage, ModifierType.Flat),
            ["Damage"] = new(Parameter.Damage, ModifierType.Flat),
            ["Health"] = new(Parameter.MaxHealth, ModifierType.Flat),
            ["CriticalChance"] = new(Parameter.CriticalChance, ModifierType.Flat),
            ["Evade"] = new(Parameter.Evade, ModifierType.Flat),
            ["ArmorPercent"] = new(Parameter.Armor, ModifierType.Increase),
            ["EvadePercent"] = new(Parameter.Evade, ModifierType.Increase),
            ["SpellDamagePercent"] = new(Parameter.SpellDamage, ModifierType.Increase),
            ["EnergyBarrierPercent"] = new(Parameter.EnergyBarrier, ModifierType.Increase),
            ["DamagePercent"] = new(Parameter.Damage, ModifierType.Increase),
            ["HealthPercent"] = new(Parameter.MaxHealth, ModifierType.Increase),
            ["CritChancePercent"] = new(Parameter.CriticalChance, ModifierType.Increase),
            ["ExtraSpellDamage"] = new(Parameter.SpellDamage, ModifierType.Multiplicative),
            ["ExtraDamage"] = new(Parameter.Damage, ModifierType.Multiplicative),
            ["ExtraCritChance"] = new(Parameter.CriticalChance, ModifierType.Multiplicative),
            ["ExtraHealth"] = new(Parameter.MaxHealth, ModifierType.Multiplicative),
            ["ExtraArmor"] = new(Parameter.Armor, ModifierType.Multiplicative),
            ["ExtraEnergyBarrier"] = new(Parameter.EnergyBarrier, ModifierType.Multiplicative),
            ["ExtraEvade"] = new(Parameter.Evade, ModifierType.Multiplicative),

        };

        public static List<IModifierInstance> CreateModifierInstances(List<IModifier> stats, object source)
        {
            List<IModifierInstance> modifiers = [];
            stats.ForEach(mod => modifiers.Add(CreateModifierInstance(mod.Parameter, mod.ModifierType, mod.BaseValue, source)));
            return modifiers;
        }

        public static List<IModifier> ItemStatsToModifiers(ItemStats stats)
        {
            List<IModifier> modifiers = [];
            var properties = stats.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (s_parameterMapping.TryGetValue(prop.Name, out var modMapping))
                {
                    float value = Convert.ToSingle(prop.GetValue(stats));

                    if (value <= 0) continue;

                    modifiers.Add(CreateModifier
                        (modMapping.Parameter,
                        modMapping.ModifierType,
                        value));
                }
            }
            return modifiers;
        }

        public static IModifier CreateModifier(Parameter parameter, ModifierType type, float baseValue)
            => new Modifier(type, parameter, baseValue);

        public static IModifierInstance CreateModifierInstance(Parameter parameter, ModifierType modifierType, float value, object source, int priority = 10)
            => new ModifierInstance(parameter, modifierType, value, source, priority);
    }
}
