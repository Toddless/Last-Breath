namespace Core.Save.Participants
{
    using System.Linq;
    using Data.SaveData;
    using Entity;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Persists every registered spawn point's population state. On restore a point recreates
    /// exactly the alive count it had at save time and resumes its respawn timers (game time);
    /// points without saved data (fresh game, points added later) fill to capacity as usual.
    /// The points themselves skip their on-ready fill while a load is pending.
    /// </summary>
    public class SpawnPointsSaveParticipant(ISpawnPointRegistry registry) : ISaveParticipant
    {
        public string SectionId => "spawnPoints";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.SpawnPoints;

        public JToken Capture() => JToken.FromObject(new SpawnPointsSaveData
        {
            Points = registry.All.Select(point => point.CaptureState()).ToList(),
        });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<SpawnPointsSaveData>() ?? new SpawnPointsSaveData();
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
