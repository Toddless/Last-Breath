namespace Core.Reputation
{
    using System.Collections.Generic;
    using Data.GameData;

    public interface IReputationDeedProcessor
    {
        IReadOnlyList<string> Catalogs { get; }
        void Apply(string catalog, GameDataFile file);
    }
}
