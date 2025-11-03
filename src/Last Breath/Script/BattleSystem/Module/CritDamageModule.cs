namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Entity;

    public class CritDamageModule : IStatModule
    {
        private readonly IEntity _owner;
        public StatModule SkillType => StatModule.CritDamage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public CritDamageModule(IEntity owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Damage.CriticalDamage;
    }
}
