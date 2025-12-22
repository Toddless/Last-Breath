namespace Core.Interfaces.Battle
{
    using System.Threading.Tasks;

    public interface IAttackContextScheduler
    {
        void Schedule(IAttackContext context);
        Task RunQueue();
        void CancelQueue();
    }
}
