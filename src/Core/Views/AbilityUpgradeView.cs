namespace Core.Views
{
    using System.Collections.Generic;

    /// <summary>Everything the ability detail window renders — pure display data, no domain object.</summary>
    public record AbilityUpgradeView(
        string AbilityId,
        string Name,
        string Cost,
        string Cooldown,
        string Description,
        IReadOnlyList<UpgradeOptionView> Options);
}
