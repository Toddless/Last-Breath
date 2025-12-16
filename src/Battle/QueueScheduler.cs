namespace Battle
{
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;

    public class QueueScheduler
    {
        private StatusEffects _skipTurnEffect = StatusEffects.Stun | StatusEffects.Freeze;
        private Queue<IEntity> FighterQueue { get; } = new();

        public event Action? QueueEmpty;

        public List<IEntity> AddFighters(List<IEntity> fighters)
        {
            var orderedFighters = fighters.OrderBy(entity => entity.Dexterity.Total).ToList();
            foreach (var fighter in orderedFighters)
            {
                if ((fighter.StatusEffects & _skipTurnEffect) != 0 || !fighter.IsAlive) continue;
                FighterQueue.Enqueue(fighter);
            }

            if (FighterQueue.Count == 0) QueueEmpty?.Invoke();
            return orderedFighters;
        }

        public IEntity? GetCurrentFighter()
        {
            try
            {
                IEntity fighter = FighterQueue.Dequeue();

                return fighter;
            }
            catch (InvalidOperationException)
            {
                QueueEmpty?.Invoke();
                return null;
            }
        }
    }
}
