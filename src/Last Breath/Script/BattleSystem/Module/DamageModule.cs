namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class DamageModule : IModule
    {
        private ICharacter _owner;
        public ModuleParameter Parameter => ModuleParameter.Damage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public DamageModule(ICharacter owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Damage.Damage;
    }
}
