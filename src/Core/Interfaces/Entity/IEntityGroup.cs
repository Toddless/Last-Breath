namespace Core.Interfaces.Entity
{
    using System.Collections.Generic;
    using Enums;

    public interface IEntityGroup
    {
        bool TryAddToGroup(IFightable entity);
        void NotifyAllInGroup(GroupNotification notification);
        void RemoveFromGroup(IFightable entity);
        List<T> GetEntitiesInGroup<T>();
    }
}
