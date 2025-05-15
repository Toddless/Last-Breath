namespace Playground.Script.Items
{
    using System;
    using System.Collections.Generic;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;
    using Playground.Script.Items.ItemData;

    public class ModifiersCreator
    {
        private record ModifierMapping(Parameter Parameter, ModifierType ModifierType, int Prioritry = ModifierPriorities.Equipment);

        private static readonly Dictionary<string, ModifierMapping> s_parameterMapping = new()
        {
            ["MaxReduceDamage"] = new(Parameter.MaxReduceDamage, ModifierType.Additive),
            ["MaxEvadeChance"] = new(Parameter.MaxEvadeChance, ModifierType.Additive),
            ["AdditionalHitChance"] = new(Parameter.AdditionalHitChance, ModifierType.Additive),
            ["Suppress"] = new(Parameter.Suppress, ModifierType.Additive),
            ["Resource"] = new(Parameter.Resource, ModifierType.Additive),
            ["ResourceRecovery"] = new(Parameter.ResourceRecovery, ModifierType.Additive),
            ["CritDamage"] = new(Parameter.CriticalDamage, ModifierType.Additive),
            ["Dexterity"] = new(Parameter.Dexterity, ModifierType.Additive),
            ["Strength"] = new(Parameter.Strength, ModifierType.Additive),
            ["Intelligence"] = new(Parameter.Intelligence, ModifierType.Additive),
            ["AllAttribute"] = new(Parameter.AllAttribute, ModifierType.Additive),
            ["Movespeed"] = new(Parameter.Movespeed, ModifierType.Additive),
            ["Armor"] = new(Parameter.Armor, ModifierType.Additive),
            ["EnergyBarrier"] = new(Parameter.EnergyBarrier, ModifierType.Additive),
            ["SpellDamage"] = new(Parameter.SpellDamage, ModifierType.Additive),
            ["Damage"] = new(Parameter.Damage, ModifierType.Additive),
            ["Health"] = new(Parameter.MaxHealth, ModifierType.Additive),
            ["CritChance"] = new(Parameter.CriticalChance, ModifierType.Additive),
            ["Evade"] = new(Parameter.Evade, ModifierType.Additive),
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

        public static List<IModifier> ItemStatsToModifier(ItemStats stats, object source)
        {
            List<IModifier> modifiers = [];
            var properties = stats.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (s_parameterMapping.TryGetValue(prop.Name, out var modMapping))
                {
                    var value = Convert.ToSingle(prop.GetValue(stats));

                    if (value <= 0) continue;

                    modifiers.Add(CreateModifier
                        (
                        modMapping.Parameter,
                        modMapping.ModifierType,
                        value,
                        source,
                        modMapping.Prioritry
                        ));
                }
            }
            return modifiers;
        }

        private static IModifier CreateModifier(Parameter parameter, ModifierType modifierType, float value, object source, int priority)
        {
            return parameter switch
            {
                Parameter.MaxReduceDamage => new MaxReduceDamageModifier(modifierType, value, source, priority),
                Parameter.MaxEvadeChance => new MaxEvadeChanceModifier(modifierType, value, source, priority),
                Parameter.AdditionalHitChance => new AdditionalHitModifier(modifierType, value, source, priority),
                Parameter.Suppress => new SuppressModifier(modifierType, value, source, priority),
                Parameter.Resource => new ResourceModifier(modifierType, value, source, priority),
                Parameter.ResourceRecovery => new ResourceRecoveryModifier(modifierType, value, source, priority),
                Parameter.CriticalDamage => new CriticalDamageModifier(modifierType, value, source, priority),
                Parameter.Dexterity => new DexterityModifier(modifierType, value, source, priority),
                Parameter.Strength => new StrengthModifier(modifierType, value, source, priority),
                Parameter.Intelligence => new IntelligenceModifier(modifierType, value, source, priority),
                Parameter.AllAttribute => new AllAtributeModifier(modifierType, value, source, priority),
                Parameter.Movespeed => new MovespeedModifier(modifierType, value, source, priority),
                Parameter.Armor => new ArmorModifier(modifierType, value, source, priority),
                Parameter.Evade => new EvadeModifier(modifierType, value, source, priority),
                Parameter.EnergyBarrier => new EnergyBarrierModifier(modifierType, value, source, priority),
                Parameter.SpellDamage => new SpellDamageModfier(modifierType, value, source, priority),
                Parameter.Damage => new DamageModifier(modifierType, value, source, priority),
                Parameter.MaxHealth => new MaxHealthModifier(modifierType, value, source, priority),
                Parameter.CriticalChance => new CriticalChanceModifier(modifierType, value, source, priority),
                _ => new MaxHealthModifier(ModifierType.Additive, 0.1f, source, priority),
            };
        }
    }
}
