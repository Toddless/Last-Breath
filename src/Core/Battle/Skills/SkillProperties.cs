namespace Core.Battle.Skills
{
    using System.Collections.Generic;

    /// <summary>Numeric parameters of a granted passive, sourced from item JSON — balance lives in data.
    /// Reads are strict: a missing key throws so the provider can report and refuse the grant,
    /// never construct a silently mis-tuned skill.</summary>
    public class SkillProperties(string skillId, IReadOnlyDictionary<string, float> values)
    {
        public static readonly SkillProperties Empty = new(string.Empty, new Dictionary<string, float>());

        /// <summary>Keys the data actually carried — what a provider checks its declared list against.</summary>
        public IEnumerable<string> Names => values.Keys;

        public float Get(string name) =>
            values.TryGetValue(name, out float value)
                ? value
                : throw new KeyNotFoundException($"Skill '{skillId}' requires property '{name}'");

        public int GetInt(string name) => (int)Get(name);
    }
}
