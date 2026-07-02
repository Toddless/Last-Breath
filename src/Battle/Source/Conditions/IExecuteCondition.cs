namespace Battle.Source.Conditions
{
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public interface IExecuteCondition
    {
        public bool ShouldExecute(IFightable target, IAttackContext? context = null);
    }
}
