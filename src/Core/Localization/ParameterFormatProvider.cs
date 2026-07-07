namespace Core.Localization
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.FormattingData;
    using Data.GameData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>Consumes the Formatting catalog; parameters absent from the JSON display as plain numbers.</summary>
    public class ParameterFormatProvider : IParameterFormatProvider, IGameDataParticipant
    {
        private readonly Dictionary<EntityParameter, ParameterUnit> _units = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.Formatting];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<ParameterFormatsData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize parameter formats");
            foreach (var entry in data.Parameters)
                _units[DataParse.ParseEnum<EntityParameter>(entry.Parameter)] = DataParse.ParseEnum<ParameterUnit>(entry.Unit);
        }

        public ParameterUnit GetUnit(EntityParameter parameter) => _units.GetValueOrDefault(parameter, ParameterUnit.Number);
    }
}
