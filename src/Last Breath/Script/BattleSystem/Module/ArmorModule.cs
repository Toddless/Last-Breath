namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class ArmorModule : IValueModule<float>
    {
        private readonly ICharacter _owner;
        public ModuleParameter Parameter => ModuleParameter.Armor;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public ArmorModule(ICharacter owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Defense.Armor;
    }
}
