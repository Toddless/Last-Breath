namespace Core.Interfaces.Entity
{
    using System.Collections.Generic;

    public interface IEntityGroup
    {
        bool TryAddToGroup(IEntity entity);
        void NotifyAllInGroup();
        void RemoveFromGroup(IEntity entity);
        List<T> GetEntitiesInGroup<T>();
    }
}
