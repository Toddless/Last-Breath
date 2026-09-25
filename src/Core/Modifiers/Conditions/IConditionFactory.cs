namespace Core.Modifiers.Conditions
{
    using Newtonsoft.Json.Linq;

    /// <summary>Builds one condition type from its record. New predicate = new factory class + DI
    /// registration (registry instead of a switch), and no consumer of conditions changes.</summary>
    public interface IConditionFactory
    {
        /// <summary>Discriminator matched against the record's "type" property.</summary>
        string Type { get; }

        /// <summary>Null when the entry is broken (missing field, empty mask) — report first. The
        /// inversion flag is common to every type and is applied by the parser, not here.</summary>
        OwnerCondition? Create(JObject json);
    }
}
