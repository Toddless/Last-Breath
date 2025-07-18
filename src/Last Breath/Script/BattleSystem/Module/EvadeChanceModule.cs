namespace Playground.Script.BattleSystem.Module
{
    using Godot;
    using Playground.Script.Enums;

    public class EvadeChanceModule : IStatModule
    {
        private readonly RandomNumberGenerator _rnd = new();
        public StatModule Type => StatModule.EvadeChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
