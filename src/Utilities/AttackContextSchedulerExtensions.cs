namespace Utilities
{
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;

    public static class AttackContextSchedulerExtensions
    {
        public static async Task DrainQueue(this IAttackContextScheduler scheduler, CancellationToken ct = default)
        {
            await foreach (var _ in scheduler.RunQueue(ct))
            {
            }
        }
    }
}
