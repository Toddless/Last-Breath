namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Skills;
    using PassiveSkills;

    /// <summary>Battle-side skill factory for item grants: maps a skill id to a constructor fed by
    /// numeric properties from item JSON — balance lives in data, this registry only wires ids to code.
    /// A missing property refuses the grant loudly instead of constructing a mis-tuned skill.</summary>
    public class PassiveSkillProvider : ISkillProvider
    {
        private static readonly Dictionary<string, Func<SkillProperties, ISkill>> s_factories = new()
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
        };

        public ISkill? CreateSkill(string id) => CreateSkill(id, SkillProperties.Empty);

        public ISkill? CreateSkill(string id, SkillProperties properties)
        {
            if (!s_factories.TryGetValue(id, out var create))
            {
                Tracker.TrackNotFound($"Passive skill factory for '{id}'", this);
                return null;
            }

            try
            {
                return create(properties);
            }
            catch (KeyNotFoundException e)
            {
                Tracker.TrackError($"Skill '{id}' not granted: {e.Message}");
                return null;
            }
        }
    }
}
