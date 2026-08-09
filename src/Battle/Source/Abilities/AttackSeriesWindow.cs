namespace Battle.Source.Abilities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Entity;

    /// <summary>
    /// One target's attack window of a series ability — the mechanics every attack-series shares:
    /// owns the reaction queue, counts the owner's attacks (reaction-spawned extras included) and
    /// fires the ability's impact riders for each. Numbers, per-hit damage and continue/stop
    /// decisions stay in the ability.
    /// </summary>
    public sealed class AttackSeriesWindow(Ability ability, IFightable owner, IBattleField field)
    {
        private readonly AttackContextScheduler _scheduler = new();
        private readonly CancellationTokenSource _cts = new();

        /// <summary>Contexts of this window must be created with this scheduler.</summary>
        public IAttackContextScheduler Scheduler => _scheduler;

        /// <summary>Owner attacks resolved in this window so far — the "every hit of the series
        /// counts as an ability hit" counter (SoA rider thresholds, max-attacks caps).</summary>
        public int OwnerAttacks { get; private set; }

        public bool IsAborted { get; private set; }

        /// <summary>
        /// Schedules one attack and fully drains it together with whatever reactions it spawns.
        /// <paramref name="onProcessed"/> runs for EVERY resolved attack of the drain (owner's and
        /// reactions'), after the owner-attack riders; return false to abort the whole window.
        /// Returns false when the attack could not be scheduled or the window aborted.
        /// </summary>
        public async Task<bool> ResolveAsync(IAttackContext context, Func<IAttackContext, Task<bool>>? onProcessed = null)
        {
            if (IsAborted) return false;
            if (!context.Schedule()) return false;

            await foreach (var processed in _scheduler.RunQueue(_cts.Token))
            {
                if (processed.Attacker.InstanceId == owner.InstanceId)
                {
                    OwnerAttacks++;
                    await ability.ApplyImpactRiders(processed.ToImpact(field, ability));
                }

                if (onProcessed == null) continue;
                if (await onProcessed(processed)) continue;

                IsAborted = true;
                await _cts.CancelAsync();
                return false;
            }

            return true;
        }
    }
}
