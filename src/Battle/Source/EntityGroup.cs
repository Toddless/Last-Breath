namespace Battle.Source
{
    using System.Linq;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Enums;

    public class EntityGroup(int maxMembers = 2) : IEntityGroup
    {
        private readonly List<IEntity> _entitiesInGroup = [];

        public bool TryAddToGroup(IEntity entity)
        {
            if (_entitiesInGroup.Count == maxMembers) return false;
            if (entity.Group != null) return false;

            _entitiesInGroup.Add(entity);
            entity.Group = this;
            return true;
        }


        public void NotifyAllInGroup(GroupNotification notification)
        {
            switch (notification)
            {
                case GroupNotification.Attacked:
                    foreach (var entity in _entitiesInGroup)
                        entity.IsFighting = true;
                    break;
            }
        }

        public List<T> GetEntitiesInGroup<T>() => _entitiesInGroup.Cast<T>().ToList();

        public void RemoveFromGroup(IEntity entity)
        {
            _entitiesInGroup.Remove(entity);
            entity.Group = null;
        }
    }
}
