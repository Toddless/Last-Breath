namespace PassiveTreeEditor.Source.Io
{
    using System.Collections.Generic;
    using Core.Data.GameData;
    using Model;

    /// <summary>
    /// Reads the tree through the same seam the game will use: a data-catalog participant. Its
    /// existence is the proof that the format needs no conversion layer — to consume the tree in the
    /// real game, register this class (or a copy of it) with <c>AddGameDataParticipant</c> and add a
    /// <c>PassiveTree</c> constant to <c>DataCatalog</c>.
    /// </summary>
    public sealed class PassiveTreeProvider : IGameDataParticipant
    {
        private readonly List<string> _issues = [];

        public IReadOnlyList<string> Catalogs => [PassiveTreeFormat.Catalog];

        public PassiveTreeDocument Tree { get; private set; } = new();

        /// <summary>Records the reader skipped, in load order. Empty means a clean file.</summary>
        public IReadOnlyList<string> Issues => _issues;

        public void Apply(string catalog, GameDataFile file)
        {
            _issues.Clear();
            Tree = PassiveTreeSerializer.Deserialize(file.Json, _issues);
        }
    }
}
