namespace Core.Views
{
    using Godot;

    /// <summary>
    /// UI-facing snapshot of one effect on a fighter, aggregated across its stacks: the icon, how many
    /// stacks are active, the remaining duration (longest among the stacks) and a description for the tooltip.
    /// </summary>
    public record EffectView(string Id, Texture2D? Icon, int Stacks, int Duration, string Description);
}
