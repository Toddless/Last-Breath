namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Godot;

    public class BlockChanceModule : IStatModule
    {
        private RandomNumberGenerator _rnd = new();
        public StatModule SkillType => StatModule.BlockChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
