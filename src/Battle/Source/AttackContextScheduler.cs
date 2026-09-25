namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle;
    using Core.Events;

    // The fuse reporter is injectable because Tracker touches Godot natives in its static ctor —
    // pure-C# tests would crash the test host the moment a fuse trips.
    public class AttackContextScheduler(Action<string>? reportFuse = null) : IAttackContextScheduler
    {
        // Safety fuses, not gameplay limits: evade/extra-hit chances are clamped below 100%,
        // so legitimate chains stay short — only a broken loop can reach these.
        private const int MaxReactionDepth = 25;
        private const int MaxAttacksPerDrain = 256;

        private readonly Queue<IAttackContext> _attackQueue = [];
        private readonly Action<string> _reportFuse = reportFuse ?? (message => Tracker.TrackError(message));

        public void Schedule(IAttackContext context)
        {
            // >=: MaxReactionDepth is the first REFUSED depth (0..Max-1 resolve) — the tests pin it.
            if (context.ReactionDepth >= MaxReactionDepth)
            {
                _reportFuse($"Reaction chain broke the depth fuse ({MaxReactionDepth}): {context.Attacker.InstanceId} -> {context.Target.InstanceId}. Attack dropped.");
                return;
            }

            _attackQueue.Enqueue(context);
        }

        public async IAsyncEnumerable<IAttackContext> RunQueue([EnumeratorCancellation] CancellationToken ct = default)
        {
            int processed = 0;
            try
            {
                while (_attackQueue.Count > 0 && !ct.IsCancellationRequested)
                {
                    if (++processed > MaxAttacksPerDrain)
                    {
                        _reportFuse($"Attack queue broke the drain fuse ({MaxAttacksPerDrain} attacks). Remaining attacks dropped.");
                        break;
                    }

                    var context = _attackQueue.Dequeue();
                    if (!context.IsValid) continue;
                    await Resolve(context);
                    yield return context;
                }
            }
            finally
            {
                // A cancelled/fused drain must not leak stale attacks into the next drain
                // (the arena reuses one scheduler across turns).
                _attackQueue.Clear();
            }
        }

        /// <summary>
        /// The two halves of one hit: the attacker joins it, whoever is hit resolves it and announces it
        /// spent. Joining opens the window in which a predicate about the target has somebody to read, and
        /// the announcement of the resolution closes it — so a join that throws before the resolution is
        /// reached closes the window itself instead of leaving the target named until the attacker's next
        /// swing, with every line written about a target counting on whatever he does in between.
        /// </summary>
        private static async Task Resolve(IAttackContext context)
        {
            try
            {
                await context.Attacker.Attack(context);
            }
            catch
            {
                context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context));
                throw;
            }

            await context.Target.ReceiveAttack(context);
        }
    }
}
