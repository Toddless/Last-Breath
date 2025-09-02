namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle.Module;

    public class ArmorModule : IStatModule
    {
        private readonly ICharacter _owner;
        public StatModule SkillType => StatModule.Armor;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public ArmorModule(ICharacter owner)
        {
            _owner = owner;
        }

        public float GetValue() => _owner.Defense.Armor;
    }
}
