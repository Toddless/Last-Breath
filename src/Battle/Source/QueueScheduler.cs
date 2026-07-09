namespace Battle.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity;

    public class QueueScheduler
    {
        private Queue<IFightable> FighterQueue { get; } = new();

        /// <summary>Fastest first: dexterity decides who acts before whom.</summary>
        public List<IFightable> AddFighters(List<IFightable> fighters)
        {
            var orderedFighters = fighters
                .Where(fighter => fighter.IsAlive)
                .OrderByDescending(fighter => fighter.Dexterity.Total)
                .ToList();
            foreach (var fighter in orderedFighters)
                FighterQueue.Enqueue(fighter);

            return orderedFighters;
        }

        /// <summary>Starts the next round when the current one is exhausted. Returns the new round
        /// order for the UI, or an empty list when nothing was refilled (fewer than two fighters
        /// left — the arena resolves the outcome, not the queue).</summary>
        public List<IFightable> RefillIfEmpty(List<IFightable> fighters)
        {
            if (FighterQueue.Count > 0) return [];
            if (fighters.Count < 2) return [];

            return AddFighters(fighters);
        }

        public bool TryGetNextFighter(out IFightable? fighter)
        {
            if (FighterQueue.Count == 0)
            {
                fighter = null;
                return false;
            }

            fighter = FighterQueue.Dequeue();
            return true;
        }
    }
}
