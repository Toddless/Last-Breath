namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity;

    public class QueueScheduler
    {
        private Queue<IFightable> FighterQueue { get; } = new();

        public event Action? QueueContainLessThenTwoFighters;

        public List<IFightable> AddFighters(List<IFightable> fighters)
        {
            var orderedFighters = fighters.OrderBy(entity => entity.Dexterity.Total).ToList();
            foreach (var fighter in orderedFighters.Where(fighter => fighter.IsAlive))
                FighterQueue.Enqueue(fighter);

            return orderedFighters;
        }

        public List<IFightable> RefillIfEmpty(List<IFightable> fighters)
        {
            if (FighterQueue.Count > 0) return [];

            if (fighters.Count >= 2) return AddFighters(fighters);

            QueueContainLessThenTwoFighters?.Invoke();
            return [];
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
