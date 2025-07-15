namespace Playground.Script.BattleSystem.Module
{
    using Godot;
    using Playground.Script.Enums;

    public class CritChanceModule : IValueModule<float>
    {
        private readonly RandomNumberGenerator _rnd = new();
        public ModuleParameter Parameter => ModuleParameter.CritChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
