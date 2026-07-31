namespace Core.Modifiers.Conditions
{
    using Data;
    using Enums;
    using Newtonsoft.Json.Linq;

    /// <summary>The two boundary states of a vital. Everything between them is a threshold.</summary>
    public enum ResourceState : byte
    {
        /// <summary>The owner holds all of it — "at full health", "mana untouched".</summary>
        Full,

        /// <summary>Nothing left — "the barrier is down", "out of mana".</summary>
        Empty,
    }

    /// <summary>
    /// "While the barrier holds", "while at full health", "while out of mana". A boundary is a discrete
    /// event (the barrier breaks once and comes back once), so it needs no band: the value either sits
    /// on the edge or it does not. The inverted forms — barrier intact, health not full — are the same
    /// record with the inversion flag.
    /// A vital the owner does not have at all is answered here and only here: it reads as empty and never
    /// as full, which is the deliberate opposite of <see cref="ResourceThresholdCondition"/> declining to
    /// answer for it — "there is no barrier" is a boundary, while "the barrier is thin" needs a barrier.
    /// </summary>
    public sealed class ResourceStateCondition(Costs resource, ResourceState state) : ResourceCondition(resource)
    {
        protected override bool Evaluate(bool wasMet)
        {
            (float current, float max) = Read();
            // A vital the owner does not have at all (no barrier) is empty, never full.
            return state == ResourceState.Empty ? current <= 0f : max > 0f && current >= max;
        }
    }

    public class ResourceStateConditionFactory : ResourceConditionFactory
    {
        public override string Type => ConditionTypes.ResourceState;

        protected override OwnerCondition Create(JObject json, Costs resource) =>
            new ResourceStateCondition(resource, EnumParser.ParseEnum<ResourceState>(json.Value<string>(ConditionFields.State) ?? string.Empty));
    }
}
