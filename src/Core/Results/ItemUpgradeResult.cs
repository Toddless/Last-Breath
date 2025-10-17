namespace Core.Results
{
    public enum ItemUpgradeResult : byte
    {
        Success = 0,
        Failure,
        CriticalSuccess,
        CriticalFailure,
        ReachedMaxLevel,
        UpgradeModeNotSet,
        WrongItemType
    }
}
