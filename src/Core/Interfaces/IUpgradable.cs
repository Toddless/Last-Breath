namespace Core.Interfaces
{
    public interface IUpgradable
    {
        int MaxUpgradeLevel { get; }
        int UpgradeLevel { get; }
        bool IsUpgradable { get; }
    }
}
