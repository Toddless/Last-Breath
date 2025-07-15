namespace Playground.Script.BattleSystem.Module
{
    using Godot;
    using Playground.Script.Enums;

    public class BlockChanceModule : IValueModule<float>
    {
        private RandomNumberGenerator _rnd = new();
        public ModuleParameter Parameter => ModuleParameter.BlockChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _rnd.Randf();
    }
}
