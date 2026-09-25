namespace Battle.Source.Presentation
{
    using System.Linq;
    using Godot;

    /// <summary>
    /// The catalog of NPC visuals keyed by NpcId (the AbilityVisualLibrary pattern): an NPC
    /// without an entry keeps the scene's placeholder frames — visuals are pure opt-in data.
    /// Lives in SharedData so every project resolves the same physical copy.
    /// </summary>
    [GlobalClass]
    public partial class NpcVisualLibrary : Resource
    {
        [Export] public Godot.Collections.Array<NpcVisualConfig> Configs { get; set; } = [];

        public NpcVisualConfig? GetConfig(string npcId) =>
            Configs.FirstOrDefault(config => config.NpcId == npcId);
    }
}
