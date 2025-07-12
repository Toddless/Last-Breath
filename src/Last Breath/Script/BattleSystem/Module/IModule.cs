namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public interface IModule
    {
        public ModuleParameter Parameter { get; }
        public DecoratorPriority Priority {  get; }

        float GetValue();
    }
}
