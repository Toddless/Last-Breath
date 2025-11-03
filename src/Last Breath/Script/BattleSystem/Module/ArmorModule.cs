namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Entity;

    public class ArmorModule : IStatModule
    {
        private readonly IEntity _owner;
        public StatModule SkillType => StatModule.Armor;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public ArmorModule(IEntity owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Defence.Armor;
    }
}
