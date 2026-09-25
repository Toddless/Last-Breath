namespace Core.Save
{
    using Data.SaveData;
    using Services;
    using Godot;
    using Newtonsoft.Json.Linq;

    /// <summary>Godot-side participant: the position lives on the player node, not on IPlayer.</summary>
    public class PlayerPlacementSaveParticipant(IPlayerAccessor playerAccessor, World.Locations.ILocationSaveCoordinator? locations = null) : ISaveParticipant
    {
        public string SectionId => "playerPlacement";
        public bool RequiredForLoad => locations != null;
        public int Version => 2;
        public int RestoreOrder => Core.Save.RestoreOrder.PlayerPlacement;

        public JToken Capture()
        {
            if (locations != null) return JToken.FromObject(locations.CapturePlacement());
            if (playerAccessor.Player is not Node2D node) return new JObject();
            return JToken.FromObject(new PlayerPlacementSaveData { X = node.GlobalPosition.X, Y = node.GlobalPosition.Y });
        }

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<PlayerPlacementSaveData>();
            if (saved == null) return;
            if (locations != null) { locations.RestorePlacement(saved); return; }
            if (playerAccessor.Player is not Node2D node) return;
            node.GlobalPosition = new Vector2(saved.X, saved.Y);
        }
    }
}
