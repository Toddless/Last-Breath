namespace Core.World.Locations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.GameData;
    using Newtonsoft.Json;

    public record LocationAddress(string LocationId, string EndpointId);
    public record LocationDefinition(string Id, string Scene);
    public record LocationConnection(LocationAddress From, LocationAddress To);
    public sealed class LocationCatalogData
    {
        public LocationAddress Start { get; init; } = new("MainWorld", "Start");
        public List<LocationDefinition> Locations { get; init; } = [];
        public List<LocationConnection> Connections { get; init; } = [];
    }

    public sealed class LocationCatalog : IGameDataParticipant
    {
        public const string MainWorldId = "MainWorld";
        public IReadOnlyList<string> Catalogs => ["Locations"];
        public LocationCatalogData Data { get; private set; } = new();

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<LocationCatalogData>(file.Json)
                ?? throw new InvalidOperationException("Empty location catalog.");
            Validate(data);
            Data = data;
        }

        public static void Validate(LocationCatalogData data)
        {
            if (data.Locations.Any(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Scene))
                || data.Locations.Select(x => x.Id).Distinct().Count() != data.Locations.Count)
                throw new InvalidOperationException("Location IDs and scene paths must be nonempty and unique.");
            var ids = data.Locations.Select(x => x.Id).ToHashSet();
            bool Valid(LocationAddress x) => ids.Contains(x.LocationId) && !string.IsNullOrWhiteSpace(x.EndpointId);
            if (!ids.Contains(MainWorldId) || !Valid(data.Start) || data.Connections.Any(x => !Valid(x.From) || !Valid(x.To)))
                throw new InvalidOperationException("Unknown location or empty endpoint.");
            if (data.Connections.Select(x => x.From).Distinct().Count() != data.Connections.Count)
                throw new InvalidOperationException("An endpoint can have only one outgoing connection.");
        }

        public LocationDefinition Get(string id) => Data.Locations.Single(x => x.Id == id);
        public LocationAddress? Destination(LocationAddress source) => Data.Connections.FirstOrDefault(x => x.From == source)?.To;
    }
}
