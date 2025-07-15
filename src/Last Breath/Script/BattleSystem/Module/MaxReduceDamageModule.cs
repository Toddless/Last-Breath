namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class MaxReduceDamageModule : IValueModule<float>
    {
        private readonly ICharacter _owner;
        public ModuleParameter Parameter => ModuleParameter.MaxReduceDamage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public MaxReduceDamageModule(ICharacter character)
        {
            _owner = character;
        }

        public float GetValue() => _owner.Defense.MaxReduceDamage;
    }
}
