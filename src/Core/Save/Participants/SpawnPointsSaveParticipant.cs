namespace Core.Save.Participants
{
    using System.Linq;
    using Data.SaveData;
    using Entity;
    using Newtonsoft.Json.Linq;

    /// <summary>Persists every registered spawn point's population state. On restore a point recreates
    /// its saved alive count and resumes respawn timers (game time); points with no saved data fill to
    /// capacity as usual. Points skip their own on-ready fill while a load is pending.</summary>
    public class SpawnPointsSaveParticipant(ISpawnPointRegistry registry) : ISaveParticipant
    {
        public string SectionId => "spawnPoints";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.SpawnPoints;

        public JToken Capture() => JToken.FromObject(new SpawnPointsSaveData
        {
            Points = registry.All.Select(point => point.CaptureState()).ToList(),
        });

        public void Restore(JToken data, int savedVersion) =>
            Apply(data.ToObject<SpawnPointsSaveData>() ?? new SpawnPointsSaveData());

        /// <summary>No section means no point was ever touched — all fill to capacity. Needed because
        /// points are scene nodes, not singleton state: a session reset can't fill them, and they skip
        /// their on-ready fill while a load is pending.</summary>
        public void RestoreWithoutSection() => Apply(new SpawnPointsSaveData());

        private void Apply(SpawnPointsSaveData saved)
        {
            var byId = saved.Points
                .GroupBy(point => point.Id)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var point in registry.All.ToList())
            {
                if (byId.TryGetValue(point.PointId, out var state)) point.RestoreState(state);
                else point.FillFresh();
            }
        }
    }
}
