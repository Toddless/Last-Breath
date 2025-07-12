namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class CritDamageModule : IModule
    {
        private ICharacter _owner;
        public ModuleParameter Parameter => ModuleParameter.CritDamage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public CritDamageModule(ICharacter owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Damage.CriticalDamage;
    }
}
