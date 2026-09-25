namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Enums;
    using PassiveSkills;

    /// <summary>Battle-side skill factory for item grants and passive-tree nodes: maps a skill id to a
    /// constructor fed by numeric properties from data — balance lives in data, this registry only wires
    /// ids to code. A missing or unreadable property refuses the grant loudly instead of constructing a
    /// mis-tuned skill.
    /// <para>Beside the named entries stands one open family: an id under
    /// <see cref="StatPassiveSkill.IdPrefix"/> is built from its own fields, so a passive that is nothing
    /// but stat lines is authored as a record and needs no entry here. A named entry always wins over the
    /// family, which is what lets a stat-shaped id later grow a factory of its own — the table is a
    /// hardcoded static, so that precedence cannot be shown by a test until the registry is data-driven.</para></summary>
    public class PassiveSkillProvider : ISkillProvider
    {
        /// <summary>Conversions whose whole content is a pair of parameters: one class answers for them, so
        /// the pairing lives in the registration and neither gets a class of its own.</summary>
        private const string IronWillId = "Passive_Skill_Iron_Will";
        private const string WindOfFreedomId = "Passive_Skill_Wind_Of_Freedom";

        private static readonly Dictionary<string, Func<RecordProperties, ISkill>> s_factories = new()
        {
            ["Passive_Skill_Regeneration"] =
                properties => new RegenerationPassiveSkill(properties.Get("percent")),
            ["Passive_Skill_Mana_Regeneration"] =
                properties => new ManaRegenerationPassiveSkill(properties.Get("percent")),
            ["Passive_Skill_Current_Health_Regeneration"] =
                properties => new CurrentHealthRegenerationPassiveSkill(properties.Get("percentFromCurrentHealth")),
            ["Passive_Skill_Mana_To_Barrier"] =
                properties => new ManaToBarrierPassiveSkill(properties.Get("percent")),
            ["Passive_Skill_Critical_Leech"] =
                properties => new CriticalLeechPassiveSkill(properties.Get("percent")),
            ["Passive_Skill_Critical_Mana_Leech"] =
                properties => new CriticalManaLeechPassiveSkill(properties.Get("percent")),
            ["Passive_Skill_No_Critical_Hits"] = _
                => new NoCriticalHitsPassiveSkill(),
            ["Passive_Skill_Mana_On_Attack"] = properties
                => new ManaOnAttackPassiveSkill(properties.Get("amount")),
            ["Passive_Skill_Porcupine"] = properties =>
                new PorcupinePassiveSkill(properties.Get("damagePercent"), properties.Get("armorPercent")),
            ["Passive_Skill_Bleeding"] = properties
                => new BleedingPassiveSkill(properties.Get("percentFromDamage"), properties.GetInt("duration"), properties.GetInt("maxStacks")),
            ["Passive_Skill_Bloodthirsty"] = properties
                => new BloodthirstyPassiveSkill(properties.GetInt("stackThreshold"), properties.Get("healPercent")),
            ["Passive_Skill_Righteous_Wrath"] = properties
                => new RighteousWrathPassiveSkill(properties.Get("percentFromDamage"), properties.GetInt("burningDuration"), properties.GetInt("stackThreshold"),
                    properties.GetInt("incinerationDuration")),
            ["Passive_Skill_Silent_Fury"] = properties
                => new SilentFuryPassive(properties.Get("chance")),
            ["Passive_Skill_Creators_Nature"] = properties
                => new CreatorsNaturePassiveSkill(properties.Get("manaRecovery"), properties.Get("healthRecovery"), properties.Get("recoveryEfficiency")),
            ["Passive_Skill_Meteor"] = properties
                => new MeteorPassiveSkill(properties.Get("damage")),
            ["Passive_Skill_Ice_Meteor"] = properties
                => new IceMeteorPassiveSkill(properties.Get("damage")),
            ["Passive_Skill_Servant_Hell"] = properties
                => new ServantHellPassiveSkill(properties.Get("chance")),
            ["Passive_Skill_Execute"] = properties
                => new ExecutePassiveSkill(properties.Get("threshold")),
            ["Passive_Skill_Vampire"] = properties
                => new VampireAttackPassiveSkill(properties.Get("percent")),
            ["Passive_Skill_Poisoned_Claws"] = properties
                => new PoisonedClaws(properties.Get("percentFromDamage"), properties.GetInt("duration")),
            ["Passive_Skill_Armor_Piercing"] = _
                => new ArmorPiercingPassiveSkill(),
            ["Passive_Skill_True_Strike"] = _
                => new TrueStrikePassiveSkill(),
            ["Passive_Skill_Soulless"] = _
                => new SoullessPassiveSkill(),
            ["Passive_Skill_Accelerator"] = properties
                => new AcceleratorPassiveSkill(properties.GetInt("amount")),
            ["Passive_Skill_Decomposition"] = properties
                => new DecompositionPassiveSkill(properties.GetInt("duration"), properties.GetInt("maxStacks"), properties.Get("reduceBy")),
            ["Passive_Skill_Incineration"] = _
                => new IncinerationPassiveSkill(),
            ["Passive_Skill_First_Strike"] = properties
                => new FirstStrikePassiveSkill(properties.Get("bonus")),
            ["Passive_Skill_Bastion"] = properties
                => new BastionPassiveSkill(properties.Get("reduce")),
            ["Passive_Skill_Unshackled"] = _
                => new UnshackledPassiveSkill(),
            ["Passive_Skill_Mana_Resonance"] = properties
                => new ManaResonancePassiveSkill(properties.Get("rate")),
            ["Passive_Skill_Chain_Attack"] = _
                => new ChainAttackPassiveSkill(),
            ["Passive_Skill_Counter_Attack"] = properties
                => new CounterAttackPassiveSkill(properties.Get("chance")),
            ["Passive_Skill_Echo"] = properties
                => new EchoPassiveSkill(properties.Get("delayedDamagePercent"), properties.GetInt("turns")),
            ["Passive_Skill_LuckyCriticalChance"] = _
                => new LuckyCriticalChancePassiveSkill(),
            ["Passive_Skill_Soul_Devouring"] = properties
                => new SoulDevouringPassiveSkill(properties.Get("barrierRecoveryAmount")),
            ["Passive_Skill_Trapped_Beast"] = properties
                => new TrappedBeastPassiveSkill(properties.Get("healthPercent"), properties.Get("damageBonus")),
            ["Passive_Skill_Mana_Burn"] = properties
                => new ManaBurnPassiveSkill(properties.Get("percentToBurn")),
            ["Passive_Skill_Burning"] = properties
                => new BurningPassiveSkill(properties.Get("percentFromDamage"), properties.GetInt("duration"), properties.GetInt("maxStacks")),
            ["Passive_Skill_Gift_From_The_Goddess"] = properties
                => new GiftFromTheGoddessPassiveSkill(properties.Get("chance")),
            ["Passive_Skill_Resonance"] = properties
                => new ResonancePassiveSkill(properties.Get("spellDamagePerStack"), properties.Get("multicastPerStack")),
            [IronWillId] = _
                => new PoolConversionPassiveSkill(IronWillId, EntityParameter.Evade, EntityParameter.Armor),
            [WindOfFreedomId] = _
                => new PoolConversionPassiveSkill(WindOfFreedomId, EntityParameter.Armor, EntityParameter.Evade),
            [AgnosticPassiveSkill.PassiveId] = properties
                => new AgnosticPassiveSkill(properties.Get("costScale")),
            [StoicismPassiveSkill.PassiveId] = _
                => new StoicismPassiveSkill(),
            [ViciousBitePassiveSkill.PassiveId] = properties
                => new ViciousBitePassiveSkill(properties.Get("perOvercap"), properties.Get("resistancePenalty")),
            [GiftOfNaturePassiveSkill.PassiveId] = properties
                => new GiftOfNaturePassiveSkill(properties.Get("elementalBonus")),
            [StrengthOfSpiritPassiveSkill.PassiveId] = properties
                => new StrengthOfSpiritPassiveSkill(properties.Get("chance"), properties.Get("percentOfMaxMana"),
                    properties.Get("recoveryPenalty")),
            [ElementalFuryPassiveSkill.PassiveId] = properties
                => new ElementalFuryPassiveSkill(properties.Get("resistancePenalty")),
            [TrinityPassiveSkill.PassiveId] = _
                => new TrinityPassiveSkill(),
        };

        /// <summary>Every id built by a factory of its own. The open stat family is not among them: it is
        /// any id under its prefix, so it can only be answered by asking rather than listed.</summary>
        public static IReadOnlyCollection<string> NamedIds => s_factories.Keys;

        public ISkill? CreateSkill(string id) => CreateSkill(id, RecordProperties.Empty);

        public ISkill? CreateSkill(string id, RecordProperties properties)
        {
            if (!s_factories.TryGetValue(id, out Func<RecordProperties, ISkill>? create))
            {
                if (!StatPassiveSkill.Owns(id))
                {
                    Tracker.TrackNotFound($"Passive skill factory for '{id}'", this);
                    return null;
                }

                create = records => StatPassiveSkill.Create(id, records);
            }

            try
            {
                return create(properties);
            }
            // A property the record does not carry, and a field name the record should not have carried:
            // the same failure from either side of the contract, refused the same way.
            catch (Exception e) when (e is KeyNotFoundException or FormatException)
            {
                Tracker.TrackError($"Skill '{id}' not granted: {e.Message}");
                return null;
            }
        }
    }
}
