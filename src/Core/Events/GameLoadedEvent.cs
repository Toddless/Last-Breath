namespace Core.Events
{
    /// <summary>A pending save file has just been applied to the freshly loaded scene. Nothing else
    /// says so: the sections replace session state each in its own way, and what a section does
    /// announce (if anything) reports its own change, not the load. Scene nodes that read that state
    /// once in their own _Ready re-read it here.</summary>
    public record GameLoadedEvent : IGameEvent;
}
