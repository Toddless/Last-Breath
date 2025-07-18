namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class DamageModule : IStatModule
    {
        private readonly ICharacter _owner;
        public StatModule ModuleType => StatModule.Damage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public DamageModule(ICharacter owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Damage.Damage;
    }
}
