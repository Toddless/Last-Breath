namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Entity;

    public class MaxReduceDamageModule : IStatModule
    {
        private readonly IEntity _owner;
        public StatModule SkillType => StatModule.MaxReduceDamage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public MaxReduceDamageModule(IEntity character)
        {
            _owner = character;
        }

        public float GetValue() => _owner.Defence.MaxReduceDamage;
    }
}
