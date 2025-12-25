namespace Battle.Source.PassiveSkills
{
    using System.Linq;
    using Core.Interfaces.Skills;
    using System.Collections.Generic;

    public abstract class PassiveFactory
    {
        public static readonly List<ISkill> Skills =
        [
            new ChainAttackPassiveSkill(),
            new CounterAttackPassiveSkill(),
            new EchoPassiveSkill(0.3f, 3),
            new ExecutePassiveSkill(0.15f),
            new ServantHellPassiveSkill(0.05f),
            new LuckyCriticalChancePassiveSkill(),
            new MulticastPassiveSkill(),
            new PoisonedClaws(0.3f, 3, 3),
            new PorcupinePassiveSkill(0.3f, 0.1f),
            new RegenerationPassiveSkill(0.05f),
            new SoulDevouringPassiveSkill(200f),
            new TrappedBeastPassiveSkill(0.05f, 0.05f),
            new VampireAttackPassiveSkill(0.15f),
            new ManaBurnPassiveSkill(0.15f),
            new BurningPassiveSkill(0.1f, 3, 5),
            new GiftFromTheGoddessPassiveSkill(0.5f)
        ];

        public static ISkill GetSkill(string skillId) => Skills.First(skill => skill.Id == skillId);
    }
}
