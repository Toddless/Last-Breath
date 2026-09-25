namespace Battle.Source.Presentation
{
    /// <summary>
    /// Resolves an NPC's look by its NpcId for every spawn path (spawn points, bosses, raids,
    /// future placed NPCs) — the spawn mechanism never knows about visuals. Null = no dedicated
    /// art, the caller keeps the placeholder frames.
    /// </summary>
    public interface INpcVisualProvider
    {
        NpcVisualConfig? GetVisual(string npcId);
    }
}
