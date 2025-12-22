namespace Battle.Source.PassiveSkills
{
    using Decorators;
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class LuckyCriticalChancePassiveSkill() : Skill(id:"Passive_Skill_LuckyCriticalChance")
    {
        private readonly EntityParameterModuleDecorator _luckyCriticalChanceDecorator = new LuckyChanceDecorator(DecoratorPriority.Strong, EntityParameter.CriticalChance);

        public override void Attach(IEntity owner)
        {
            owner.Parameters.AddModuleDecorator(_luckyCriticalChanceDecorator);
        }

        public override void Detach(IEntity owner)
        {
            owner.Parameters.RemoveModuleDecorator(_luckyCriticalChanceDecorator.Id, EntityParameter.CriticalChance);
        }

        public override ISkill Copy() => new LuckyCriticalChancePassiveSkill();

        public override bool IsStronger(ISkill skill) => false;
    }
}
