namespace Core.Extensions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Battle;

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
