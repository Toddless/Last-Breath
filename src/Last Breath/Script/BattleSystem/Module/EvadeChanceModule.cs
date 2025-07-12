namespace Playground.Script.BattleSystem.Module
{
    using Godot;
    using Playground.Script.Enums;

    public class EvadeChanceModule : IModule
    {
        private readonly RandomNumberGenerator _rnd = new();
        public ModuleParameter Parameter => ModuleParameter.EvadeChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
