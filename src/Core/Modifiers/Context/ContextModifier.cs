namespace Core.Modifiers.Context
{
    using System;
    using Enums;

    public abstract class ContextModifier(Priority priority, string id)
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public Priority Priority { get; } = priority;
    }
}
