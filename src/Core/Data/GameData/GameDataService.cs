namespace Core.Data.GameData
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The load orchestrator (mirror of SaveManager): reads every participant's catalogs and
    /// feeds the files in. Error policy lives here and only here: a broken catalog or file is
    /// reported through <see cref="LoadFailed"/> and skipped, the rest of the data still loads.
    /// Pure C# — failures reach the Tracker via the DI wiring, not from this class.
    /// </summary>
    public class GameDataService(IGameDataSource source, IEnumerable<IGameDataParticipant> participants) : IGameDataService
    {
        public event Action<string, Exception>? LoadFailed;

        public void LoadAll()
        {
            foreach (var participant in participants)
                foreach (string catalog in participant.Catalogs)
                    LoadCatalog(participant, catalog);
        }

        private void LoadCatalog(IGameDataParticipant participant, string catalog)
        {
            IReadOnlyList<GameDataFile> files;
            try
            {
                files = source.ReadCatalog(catalog);
            }
            catch (Exception e)
            {
                LoadFailed?.Invoke(catalog, e);
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    participant.Apply(catalog, file);
                }
                catch (Exception e)
                {
                    LoadFailed?.Invoke($"{catalog}/{file.FileName}", e);
                }
            }
        }
    }
}
