namespace LastBreath.Script.BattleSystem.Module
{
    using LastBreath.Script;
    using LastBreath.Script.Enums;

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
