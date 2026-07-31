namespace Core.Modifiers.Conditions
{
    using Interfaces;
    using Newtonsoft.Json.Linq;

    /// <summary>Turns a condition record into a live predicate through the factory registry.</summary>
    public interface IConditionParser
    {
        /// <summary>
        /// True when the record produced a predicate, or when there was no record at all
        /// (<paramref name="token"/> null or json null — the line is simply unconditional and
        /// <paramref name="condition"/> comes back null). False means the record is broken or names an
        /// unknown type: it has already been reported and the caller must DROP the whole line rather
        /// than let it apply unconditionally.
        /// </summary>
        bool TryParse(JToken? token, out ICondition? condition);
    }
}
