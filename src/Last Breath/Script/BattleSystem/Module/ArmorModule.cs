namespace LastBreath.Script.BattleSystem.Module
{
    using LastBreath.Script;
    using LastBreath.Script.Enums;

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
