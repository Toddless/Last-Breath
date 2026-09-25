namespace Core.Reputation
{
    using Godot;

    /// <summary>
    /// Answers "did anybody learn about a deed at this position?". The minimal model: a living
    /// bystander NPC of a reputation-bearing faction within the radius, or anyone who fled the
    /// current battle (a fleeing survivor always spreads the word — chase him down or pay).
    /// Rumor propagation and per-faction knowledge live behind this contract later.
    /// </summary>
    public interface IWitnessQuery
    {
        bool HasWitness(Vector2 position, float radius, string? excludeInstanceId = null);
    }
}
