namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.Enums;

    public interface IModuleDecorator<TKey, TModule>
        where TKey : notnull
    {
        TKey Type { get; }
        DecoratorPriority Priority { get; }

        void ChainModule(TModule inner);
    }
}
