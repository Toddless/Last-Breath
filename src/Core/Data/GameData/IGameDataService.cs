namespace Core.Data.GameData
{
    using System;

    /// <summary>Loads all registered participants' catalogs. Called ONCE by the project bootstrap.</summary>
    public interface IGameDataService
    {
        /// <summary>Raised per failed catalog read or file apply; loading continues with the rest.</summary>
        event Action<string, Exception>? LoadFailed;

        void LoadAll();
    }
}
