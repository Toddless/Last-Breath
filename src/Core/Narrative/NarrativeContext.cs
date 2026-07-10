namespace Core.Narrative
{
    using Enums;

    /// <summary>
    /// The situation a narrative condition/action is evaluated in: who the player is talking to.
    /// The player itself is ambient (services inject IPlayerAccessor); outside a conversation
    /// use <see cref="Empty"/> — npc-scoped entries then simply aren't met.
    /// </summary>
    public record NarrativeContext(string? NpcInstanceId = null, Fractions? NpcFaction = null)
    {
        public static readonly NarrativeContext Empty = new();
    }
}
