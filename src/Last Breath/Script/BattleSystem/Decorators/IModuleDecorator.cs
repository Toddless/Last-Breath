namespace LastBreath.Script.BattleSystem.Decorators
{
    using Contracts.Enums;

    public interface IModuleDecorator<TKey, TModule>
        where TKey : notnull
    {
        string Id { get; }
        TKey SkillType { get; }
        DecoratorPriority Priority { get; }

        void ChainModule(TModule inner);
    }
}
