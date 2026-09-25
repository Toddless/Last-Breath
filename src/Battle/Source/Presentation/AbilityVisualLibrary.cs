namespace Battle.Source.Presentation
{
    using System.Linq;
    using Godot;

    /// <summary>
    /// The catalog of ability visuals: one SpriteFrames with all VFX clips plus a config per ability.
    /// Abilities without a config play the old way (cast pose + hurt) — visuals are pure opt-in data.
    /// </summary>
    [GlobalClass]
    public partial class AbilityVisualLibrary : Resource
    {
        [Export] public SpriteFrames? Frames { get; set; }
        [Export] public Godot.Collections.Array<AbilityVisualConfig> Configs { get; set; } = [];

        public AbilityVisualConfig? GetConfig(string abilityId) =>
            Configs.FirstOrDefault(config => config.AbilityId == abilityId);
    }
}
