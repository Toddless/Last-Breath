namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Entity;

    public class DamageModule : IStatModule
    {
        private readonly IEntity _owner;
        public StatModule SkillType => StatModule.Damage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public DamageModule(IEntity owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Damage.Damage;
    }
}
