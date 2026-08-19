namespace Core.PassiveTree
{
    using System.Collections.Generic;
    using Data.GameData;

    /// <summary>Reads the tree through the ordinary data seam: one catalog, one file, no conversion
    /// layer. A record the reader can't make sense of is reported and skipped, so one bad node never costs the whole tree.</summary>
    public sealed class PassiveTreeProvider : IPassiveTreeProvider, IGameDataParticipant
    {
        private readonly List<string> _issues = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.PassiveTree];

        public PassiveTreeDocument Tree { get; private set; } = new();

        public IReadOnlyList<string> Issues => _issues;

        public void Apply(string catalog, GameDataFile file)
        {
            _issues.Clear();
            Tree = PassiveTreeSerializer.Deserialize(file.Json, _issues);

            foreach (string issue in _issues)
                Tracker.TrackError($"{catalog}/{file.FileName}: {issue}");
        }
    }
}
