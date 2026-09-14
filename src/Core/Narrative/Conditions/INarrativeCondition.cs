namespace Core.Narrative.Conditions
{
    /// <summary>
    /// A data-driven predicate over the world state, evaluated on demand. One vocabulary serves
    /// three consumers: quest availability, objective completion and dialogue option gating.
    /// Distinct from Interfaces.ICondition — that one is a live, owner-attached combat predicate.
    /// </summary>
    public interface INarrativeCondition
    {
        /// <summary>Explicit opt-in for repeated admission checks without writes or random rolls.</summary>
        bool IsPreviewSafe => false;

        bool IsMet(NarrativeContext context);
    }
}
