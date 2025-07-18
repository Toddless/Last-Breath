namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.Enums;

    public interface IModuleDecorator<TModule>
    {
        DecoratorPriority Priority { get; }

        void ChainModule(TModule module);
    }
}
