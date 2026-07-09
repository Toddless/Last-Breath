namespace Battle.Internal.Save
{
    using Core.Data.SaveData;
    using Core.Save;
    using Core.Services;
    using Godot;
    using Newtonsoft.Json.Linq;

    /// <summary>Godot-side participant: the position lives on the player node, not on IPlayer.</summary>
    public class PlayerPlacementSaveParticipant(IPlayerAccessor playerAccessor) : ISaveParticipant
    {
        public string SectionId => "playerPlacement";
        public int Version => 1;
        public int RestoreOrder => Core.Save.RestoreOrder.PlayerPlacement;

        public JToken Capture()
        {
            if (playerAccessor.Player is not Node2D node) return new JObject();
            return JToken.FromObject(new PlayerPlacementSaveData { X = node.GlobalPosition.X, Y = node.GlobalPosition.Y });
        }

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<PlayerPlacementSaveData>();
            if (saved == null || playerAccessor.Player is not Node2D node) return;
            node.GlobalPosition = new Vector2(saved.X, saved.Y);
        }
    }
}
