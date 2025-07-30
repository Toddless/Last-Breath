namespace LastBreath.Script.BattleSystem.Module
{
    using Godot;
    using LastBreath.Script.Enums;

    public class BlockChanceModule : IStatModule
    {
        private RandomNumberGenerator _rnd = new();
        public StatModule SkillType => StatModule.BlockChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
