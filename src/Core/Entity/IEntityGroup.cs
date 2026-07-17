namespace Core.Entity
{
    using System.Collections.Generic;
    using Enums;

    public interface IEntityGroup
    {
        bool TryAddToGroup(IFightable entity);

        /// <summary>Membership that must not fail: battle-side semantics (a summon shares the
        /// summoner's side) beat the squad's world capacity cap.</summary>
        void ForceAddToGroup(IFightable entity);
        void NotifyAllInGroup(GroupNotification notification);
        void RemoveFromGroup(IFightable entity);
        List<T> GetEntitiesInGroup<T>();
    }
}
