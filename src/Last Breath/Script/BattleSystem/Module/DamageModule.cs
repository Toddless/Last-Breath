namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using LastBreath.Script;

    public class DamageModule : IStatModule
    {
        private readonly ICharacter _owner;
        public StatModule SkillType => StatModule.Damage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public DamageModule(ICharacter owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Damage.Damage;
    }
}
