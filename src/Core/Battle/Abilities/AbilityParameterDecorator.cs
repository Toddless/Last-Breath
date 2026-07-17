namespace Core.Battle.Abilities
{
    using Enums;

    /// <summary>
    /// A single value transformation on top of an ability parameter (upgrades, temporary effects).
    /// Lives in the ability's <see cref="AbilityParameterSet"/>; chained by <see cref="Priority"/>.
    /// </summary>
    public abstract class AbilityParameterDecorator(string parameter, Priority priority, string id, string source)
    {
        public string Id { get; } = id;
        public string Source { get; } = source;
        public string Parameter { get; } = parameter;
        public Priority Priority { get; } = priority;

        public abstract float Decorate(float value);

        /// <summary>Same-Id collision policy: the existing decorator wins unless overridden.</summary>
        public virtual bool IsStronger(AbilityParameterDecorator decorator) => decorator.Id == Id;
    }
}
