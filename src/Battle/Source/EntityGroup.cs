namespace Battle.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity;
    using Core.Enums;

    public class EntityGroup(int maxMembers = 2) : IEntityGroup
    {
        private readonly List<IFightable> _entitiesInGroup = [];

        public bool TryAddToGroup(IFightable entity)
        {
            if (_entitiesInGroup.Count == maxMembers) return false;
            if (entity.Group != null) return false;

            _entitiesInGroup.Add(entity);
            entity.Group = this;
            return true;
        }

        /// <summary>Battle-side membership beats the squad's world capacity: a summon must share
        /// the summoner's side even when the world group was authored full.</summary>
        public void ForceAddToGroup(IFightable entity)
        {
            if (entity.Group != null) return;
            _entitiesInGroup.Add(entity);
            entity.Group = this;
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

        public void RemoveFromGroup(IFightable entity)
        {
            _entitiesInGroup.Remove(entity);
            entity.Group = null;
        }
    }
}
