namespace Core.Interfaces.Battle
{
    using System.Threading.Tasks;
    using Entity;

    public interface ICombatScheduler
    {
        void Schedule(IAttackContext context);
        Task RunQueue();
        void CancelQueue();
    }
}
