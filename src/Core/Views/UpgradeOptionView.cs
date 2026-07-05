namespace Core.Views
{
    /// <summary>One upgrade choice inside a tier. Selected = currently applied to the ability.</summary>
    public record UpgradeOptionView(string UpgradeInstanceId, string DisplayName, string Description, int Tier, bool Selected);
}
