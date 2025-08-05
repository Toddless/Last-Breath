namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using LastBreath.Script;

    public class MaxReduceDamageModule : IStatModule
    {
        private readonly ICharacter _owner;
        public StatModule SkillType => StatModule.MaxReduceDamage;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public MaxReduceDamageModule(ICharacter character)
        {
            _owner = character;
        }

        public float GetValue() => _owner.Defense.MaxReduceDamage;
    }
}
