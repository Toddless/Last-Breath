namespace Core.Data
{
    using System.Collections.Generic;
    using Items;

    /// <summary>Read side of the equip blueprint store. Blueprints replace equip templates in the
    /// item dictionary: minting is the only way to turn one into a live item.</summary>
    public interface IEquipBlueprintProvider
    {
        /// <summary>Null when the id has no equip blueprint — a legal probe (the facade falls back
        /// to the plain item store), so no error is reported here.</summary>
        EquipItemBlueprint? GetBlueprint(string id);

        IEnumerable<EquipItemBlueprint> AllBlueprints { get; }
    }
}
