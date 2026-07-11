namespace Core.Data.SaveData
{
    using System.Collections.Generic;

    public class SpawnPointsSaveData
    {
        public List<SpawnPointSaveData> Points { get; set; } = [];
    }

    /// <summary>One point's population state: how many slots were alive at save time and when
    /// the pending replacements are due, in absolute game minutes (the world clock is saved too).</summary>
    public class SpawnPointSaveData
    {
        public string Id { get; set; } = string.Empty;
        public int Alive { get; set; }
        public List<double> PendingDueMinutes { get; set; } = [];
    }
}
