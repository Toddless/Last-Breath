namespace Battle.Source
{
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using System.Linq;

    public class QueueScheduler
    {
        private readonly List<IEntity> _entities = [];
        private readonly LinkedList<IEntity> _queue = [];

        public void AddMembers<T>(IEnumerable<T> entities)
            where T : IEntity, IFightable
        {
            // TODO: Add to UI
            var fighters = entities.ToList();

            foreach (var entity in fighters)
                //entity.Dead += OnEntityDead;

            _entities.AddRange(entities as IEnumerable<IEntity> ?? []);
            DecideQueueOrder(fighters);
        }

        public void AddMember<T>(T entity)
            where T : IEntity, IFightable
        {
            // TODO: Add to UI
         //   entity.Dead += OnEntityDead;
            _entities.Add(entity);
            _queue.AddLast(entity);
        }

        public T? GetNext<T>()
            where T : IEntity, IFightable
        {
            var nextTurn = _queue.First?.Value;
            _queue.RemoveFirst();
            if (nextTurn != null)
                _queue.AddLast(nextTurn);
            return (T?)nextTurn;
        }

        private void DecideQueueOrder<T>(IEnumerable<T> entities)
            where T : IEntity, IFightable
        {
            foreach (var entity in entities)
                _queue.AddLast(entity);
        }

        private void OnEntityDead(IEntity entity)
        {
            // TODO: Remove from ui
           // entity.Dead -= OnEntityDead;
            _queue.Remove(entity);
        }
    }
}
