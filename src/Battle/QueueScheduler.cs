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

        public void AddFighters(List<IEntity> fighters)
        {
            foreach (var fighter in fighters.OrderBy(entity => entity.Dexterity.Total))
            {
                if (!fighter.IsAlive) continue;
                if ((fighter.StatusEffects & _skipTurnEffect) != 0) continue;
                FighterQueue.Enqueue(fighter);
            }

            if (FighterQueue.Count == 0) QueueEmpty?.Invoke();
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
