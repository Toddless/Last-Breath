namespace Core.Results
{
    public enum ItemUpgradeResult : byte
    {
        Success = 0,
        Failure,
        ReachedMaxLevel,

        /// <summary>Rejected before the roll: the inventory doesn't cover the cost. Nothing was consumed.</summary>
        NotEnoughResources
    }
}
