namespace Core.Interfaces.Entity
{
    using System.Collections.Generic;
    using Enums;

    public interface IEntityGroup
    {
        bool TryAddToGroup(IEntity entity);
        void NotifyAllInGroup(GroupNotification notification);
        void RemoveFromGroup(IEntity entity);
        List<T> GetEntitiesInGroup<T>();
    }
}
