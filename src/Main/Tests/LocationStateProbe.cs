namespace LastBreath.Tests
{
    using Core.World.Locations;
    using Godot;
    using Newtonsoft.Json.Linq;

    public partial class LocationStateProbe : Node, ILocationStateParticipant
    {
        [Export] public string ObjectId { get; set; } = "";
        public int Contents { get; set; } = 7;
        public double ElapsedMinutes { get; private set; }
        public JToken CaptureLocationState() => new JObject { ["contents"] = Contents, ["elapsed"] = ElapsedMinutes };
        public void RestoreLocationState(JToken state)
        {
            Contents = (int)state["contents"]!;
            ElapsedMinutes = (double)state["elapsed"]!;
        }
        public void ReconcileElapsed(double gameMinutes) => ElapsedMinutes += gameMinutes;
    }
}
