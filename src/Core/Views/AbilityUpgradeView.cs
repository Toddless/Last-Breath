namespace Core.Views
{
    using System.Collections.Generic;

    /// <summary>Everything the ability detail window renders — pure display data, no domain object.
    /// The numbers (cost, cooldown, the description's placeholders) are the ability's LIVE ones, so the
    /// augments in <see cref="Worn"/> are already counted in them.</summary>
    /// <param name="Worn">The augments sitting in the ability's sockets, one row each.</param>
    public record AbilityUpgradeView(
        string AbilityId,
        string Name,
        string Cost,
        string Cooldown,
        string Description,
        IReadOnlyList<UpgradeOptionView> Worn);
}
