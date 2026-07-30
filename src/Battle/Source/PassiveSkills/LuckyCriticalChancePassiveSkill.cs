namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    public class LuckyCriticalChancePassiveSkill() : Skill(id: "Passive_Skill_LuckyCriticalChance")
    {
        private readonly EntityParameterModuleDecorator _luckyCriticalChanceDecorator = new LuckyChanceDecorator(Priority.Strong, EntityParameter.CriticalChance);

        public override void Attach(IFightable owner)
        {
            owner.Parameters.AddModuleDecorator(_luckyCriticalChanceDecorator);
        }

        public override void Detach(IFightable owner)
        {
            owner.Parameters.RemoveModuleDecorator(_luckyCriticalChanceDecorator.Id, EntityParameter.CriticalChance);
        }

        public override ISkill Copy() => new LuckyCriticalChancePassiveSkill();

        /// <summary>Parameterless: the decorator rerolls the same crit chance for every instance,
        /// so no instance can be stronger than another.</summary>
        public override bool IsStronger(ISkill skill) => false;
    }
}
