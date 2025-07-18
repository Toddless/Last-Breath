namespace Playground.Script.BattleSystem.Module
{
    using Godot;
    using Playground.Script.Enums;

    public class BlockChanceModule : IStatModule
    {
        private RandomNumberGenerator _rnd = new();
        public StatModule ModuleType => StatModule.BlockChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
