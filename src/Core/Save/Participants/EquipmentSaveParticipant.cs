namespace Core.Save.Participants
{
    using System.Linq;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>
    /// Runs at <see cref="RestoreOrder.Items"/>: equipping re-attaches grants and modifier
    /// sources, so everything derived (parameters, mastery bonus levels) settles before vitals.
    /// </summary>
    public class EquipmentSaveParticipant(IPlayerAccessor playerAccessor, EquipItemSaveConverter converter) : ISaveParticipant
    {
        public string SectionId => "equipment";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Items;

        public JToken Capture()
        {
            var equipment = playerAccessor.Player?.Equipment;
            if (equipment == null) return new JArray();
            return JToken.FromObject(equipment.Equipped.Values.Select(converter.ToData).ToList());
        }

        public void Restore(JToken data, int savedVersion)
        {
            var equipment = playerAccessor.Player?.Equipment;
            var saved = data.ToObject<System.Collections.Generic.List<EquipItemSaveData>>();
            if (equipment == null || saved == null) return;

            foreach (var piece in equipment.Equipped.Keys.ToList())
                equipment.TryUnequip(piece, out _);

            foreach (var itemData in saved)
                equipment.TryEquip(converter.FromData(itemData), out _);
        }
    }
}
