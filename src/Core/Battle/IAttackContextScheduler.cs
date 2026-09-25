namespace Core.Battle
{
    using System.Collections.Generic;
    using System.Threading;

    public interface IAttackContextScheduler
    {
        void Schedule(IAttackContext context);
        IAsyncEnumerable<IAttackContext> RunQueue(CancellationToken ct = default);
    }
}
