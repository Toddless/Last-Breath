namespace Core.Modifiers.Context
{
    using System;
    using Enums;

    public abstract class ContextModifier(ContextModifierPriority priority, string id)
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        /// <summary>Where the modifier runs in its pipeline. The class picks the slot its rule belongs in;
        /// the setter lets the line that stands behind an instance place it elsewhere, which is how a
        /// whole source of modifiers can be ordered as a group. Settable inside the assembly only, and
        /// meant to be taken before the modifier is registered: a handler re-sorts a pipeline when
        /// something is added to it and at no other time, so a slot taken afterwards decides nothing until
        /// the next modifier arrives.</summary>
        public ContextModifierPriority Priority { get; internal set; } = priority;
    }
}
