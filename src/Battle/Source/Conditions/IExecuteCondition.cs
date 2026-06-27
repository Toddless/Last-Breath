namespace Battle.Source.Conditions
{
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public interface IExecuteCondition
    {
        public bool ShouldExecute(IEntity target, IAttackContext? context = null);
    }
}
