namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class MaxReduceDamageModule : IStatModule
    {
        private readonly ICharacter _owner;
        public StatModule Type => StatModule.MaxReduceDamage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public MaxReduceDamageModule(ICharacter character)
        {
            _owner = character;
        }

        public float GetValue() => _owner.Defense.MaxReduceDamage;
    }
}
