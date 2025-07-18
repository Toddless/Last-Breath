namespace Playground.Script.BattleSystem.Module
{
    using Godot;
    using Playground.Script.Enums;

    public class AdditionalHitChanceModule : IStatModule
    {
        private readonly RandomNumberGenerator _rnd = new();
        public StatModule ModuleType => StatModule.AdditionalAttackChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
