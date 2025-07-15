namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public interface IValueModule<T>
    {
        public ModuleParameter Parameter { get; }
        public DecoratorPriority Priority {  get; }

        T GetValue();
    }
}
