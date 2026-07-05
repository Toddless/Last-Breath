namespace Battle.Internal.Tools
{
    using Core.Enums;
    using Godot;

    /// <summary>
    /// Inspector-editable spec for one parameter modifier applied to spawned NPCs.
    /// A list of these on <see cref="NpcSpawnTool"/> defines the generated NPC's stat modifiers
    /// (e.g. Health / Flat / 500, or Damage / Increase / 0.5).
    /// </summary>
    [GlobalClass]
    public partial class NpcStatSpec : Resource
    {
        [Export] public EntityParameter Parameter { get; set; } = EntityParameter.Health;
        [Export] public ModifierValueType ValueType { get; set; } = ModifierValueType.Flat;
        [Export] public float Value { get; set; }
    }
}
