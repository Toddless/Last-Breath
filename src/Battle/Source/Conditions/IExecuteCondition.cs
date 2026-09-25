namespace Battle.Source.Conditions
{
    using Core.Battle;
    using Core.Entity;

    public interface IExecuteCondition
    {
        public bool ShouldExecute(IFightable target, IAttackContext? context = null);
    }
}
