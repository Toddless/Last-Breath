namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class MaxReduceDamageModule : IStatModule
    {
        private readonly ICharacter _owner;
        public StatModule ModuleType => StatModule.MaxReduceDamage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public MaxReduceDamageModule(ICharacter character)
        {
            _owner = character;
        }

        public float GetValue() => _owner.Defense.MaxReduceDamage;
    }
}
