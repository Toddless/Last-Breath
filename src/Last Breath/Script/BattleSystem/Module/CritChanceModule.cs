namespace Playground.Script.BattleSystem.Module
{
    using Godot;
    using Playground.Script.Enums;

    public class CritChanceModule : IStatModule
    {
        private readonly RandomNumberGenerator _rnd = new();
        public StatModule Type => StatModule.CritChance;
        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
