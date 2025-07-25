namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class CritDamageModule : IStatModule
    {
        private readonly ICharacter _owner;
        public StatModule SkillType => StatModule.CritDamage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public CritDamageModule(ICharacter owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Damage.CriticalDamage;
    }
}
