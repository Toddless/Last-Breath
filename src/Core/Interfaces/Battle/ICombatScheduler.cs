namespace Core.Interfaces.Battle
{
    using System.Threading.Tasks;

    public interface ICombatScheduler
    {
        void Schedule(IAttackContext context);
        Task RunQueue();
        void CancelQueue();
    }
}
