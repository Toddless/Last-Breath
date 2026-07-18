namespace Battle.Source.Presentation
{
    using Godot;

    /// <summary>
    /// Loads the shared NPC visual catalog lazily on first use (never in the constructor —
    /// ResourceLoader outside the Godot runtime is fatal for the test projects). A missing
    /// library file simply means every NPC keeps its placeholder frames.
    /// </summary>
    public class NpcVisualProvider : INpcVisualProvider
    {
        private const string LibraryPath = "res://Data/Shared/Assets/Npc/Animations/NpcVisuals.tres";

        private bool _loaded;
        private NpcVisualLibrary? _library;

        public NpcVisualConfig? GetVisual(string npcId)
        {
            if (!_loaded)
            {
                _loaded = true;
                _library = ResourceLoader.Exists(LibraryPath) ? ResourceLoader.Load<NpcVisualLibrary>(LibraryPath) : null;
            }

            return _library?.GetConfig(npcId);
        }
    }
}
