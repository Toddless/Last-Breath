namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class QueueScheduler
    {
        private readonly StatusEffects _skipTurnEffect = StatusEffects.Stun | StatusEffects.Freeze;
        private Queue<IEntity> FighterQueue { get; } = new();

        public event Action? QueueContainLessThenTwoFighters;

        public List<IEntity> AddFighters(List<IEntity> fighters)
        {
            var orderedFighters = fighters.OrderBy(entity => entity.Dexterity.Total).ToList();
            foreach (var fighter in orderedFighters)
            {
                if ((fighter.StatusEffects & _skipTurnEffect) != 0 || !fighter.IsAlive) continue;
                FighterQueue.Enqueue(fighter);
            }

            return orderedFighters;
        }

        public List<IEntity> RefillIfEmpty(List<IEntity> fighters)
        {
            if (FighterQueue.Count > 0) return [];

            if (fighters.Count >= 2) return AddFighters(fighters);

            QueueContainLessThenTwoFighters?.Invoke();
            return [];
        }

        public bool TryGetNextFighter(out IEntity? fighter)
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
