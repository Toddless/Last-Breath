namespace LastBreath.Script.BattleSystem.Module
{
    using Godot;
    using LastBreath.Script.Enums;

    public class CritChanceModule : IStatModule
    {
        private readonly RandomNumberGenerator _rnd = new();
        public StatModule SkillType => StatModule.CritChance;
        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
