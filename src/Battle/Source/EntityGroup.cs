namespace Battle.Source
{
    using Godot;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using System.Linq;

    public class EntityGroup : IEntityGroup
    {
        // TODO: Уведомления для группы
        // 1. Член группы был атакован
        // 2. Член группы обнаружил противника
        // 3. Член группы инициировал нападение

        private readonly int _maxMembers;
        private readonly List<IEntity> _entitiesInGroup = [];

        public EntityGroup()
        {
            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            _maxMembers = rnd.RandiRange(2, 4);
        }


        public bool TryAddToGroup(IEntity entity)
        {
            if (_entitiesInGroup.Count == _maxMembers) return false;
            if (entity.Group != null) return false;

            _entitiesInGroup.Add(entity);
            entity.Group = this;
            return true;
        }


        public void NotifyAllInGroup()
        {
            foreach (var entity in _entitiesInGroup)
            {
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
