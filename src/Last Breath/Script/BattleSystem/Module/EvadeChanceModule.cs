namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Godot;

    public class EvadeChanceModule : IStatModule
    {
        private readonly RandomNumberGenerator _rnd = new();
        public StatModule SkillType => StatModule.EvadeChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
