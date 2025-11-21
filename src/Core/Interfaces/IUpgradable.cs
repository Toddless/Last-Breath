namespace Core.Interfaces
{
    public interface IUpgradable
    {
        int MaxUpgradeLevel { get; }
        int CurrentLevel { get; }
        bool IsUpgradable { get; }
    }
}
