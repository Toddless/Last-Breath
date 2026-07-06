namespace Battle.Source
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Core.Interfaces.Battle;

    public class AttackContextScheduler : IAttackContextScheduler
    {
        private readonly Queue<IAttackContext> _attackQueue = [];

        public void Schedule(IAttackContext context) => _attackQueue.Enqueue(context);

        public async IAsyncEnumerable<IAttackContext> RunQueue([EnumeratorCancellation] CancellationToken ct = default)
        {
            while (_attackQueue.Count > 0 && !ct.IsCancellationRequested)
            {
                var context = _attackQueue.Dequeue();
                if (!context.IsValid) continue;
                await context.Attacker.Attack(context);
                await context.Target.ReceiveAttack(context);
                yield return context;
            }
        }
    }
}
