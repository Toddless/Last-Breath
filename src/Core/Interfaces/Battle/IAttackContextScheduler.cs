namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IAttackContextScheduler
    {
        void Schedule(IAttackContext context);
        IAsyncEnumerable<IAttackContext> RunQueue(CancellationToken ct = default);
    }
}
