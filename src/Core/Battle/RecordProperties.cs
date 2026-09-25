namespace Core.Battle
{
    using System.Collections.Generic;

    /// <summary>Named numbers a data record hands to the factory that builds from it — a passive skill,
    /// a granted effect, an augment record. Reads are strict: a missing key throws so the factory can
    /// report and refuse, never build a silently mis-tuned thing.</summary>
    public class RecordProperties(string recordId, IReadOnlyDictionary<string, float> values)
    {
        public static readonly RecordProperties Empty = new(string.Empty, new Dictionary<string, float>());

        /// <summary>Keys the record actually carried — what a factory checks its declared list against.</summary>
        public IEnumerable<string> Names => values.Keys;

        public float Get(string name) =>
            values.TryGetValue(name, out float value)
                ? value
                : throw new KeyNotFoundException($"Record '{recordId}' requires property '{name}'");

        public int GetInt(string name) => (int)Get(name);
    }
}
