namespace Battle.Source.Abilities.PassiveSkills
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Skills;

    public abstract class PassiveFactory
    {
        public static readonly List<ISkill> Skills =
        [
            new ChainAttackPassiveSkill("Passive_Skill_Chain_Attack"),
            new CounterAttackPassiveSkill("Passive_Skill_Counter_Counter"),
            new EchoPassiveSkill("Passive_Skill_Echo", 0.3f, 3),
            new ExecutePassiveSkill("Passive_Skill_Execute", 0.15f),
            new HelServantPassiveSkill("Passive_Skill_Hell_Servant", 0.05f),
            new LuckyCriticalChancePassiveSkill("Passive_Skill_LuckyCriticalChance"),
            new MulticastPassiveSkill("Passive_Skill_Multicast"),
            new PoisonedClaws(0.3f, 5, 3),
            new PorcupinePassiveSkill("Passive_Skill_Porcupine", 0.3f, 0.1f),
            new RegenerationPassiveSkill("Passive_Skill_Regeneration", 50f),
            new SoulDevouringPassiveSkill("Passive_Skill_SoulDevouring", 200f),
            new TrappedBeastPassiveSkill("Passive_Skill_Trapped_Beast", 0.05f, 0.05f),
            new VampireAttackPassiveSkill("Passive_Skill_Vampier", 0.15f)
        ];


        public static ISkill GetSkill(string skillId) => Skills.First(skill => skill.Id == skillId);
    }
}
