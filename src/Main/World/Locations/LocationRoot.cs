namespace LastBreath.World.Locations
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Views.UI;
    using Core.World.Locations;
    using Godot;

    [GlobalClass]
    public partial class LocationRoot : Node2D
    {
        [Export] public string LocationId { get; set; } = LocationCatalog.MainWorldId;
        public bool IsPreparing { get; set; } = true;
        public double? ReconciliationMinutes { get; set; }
        public HashSet<string> AuthoredObjectIds { get; } = [];
        public override void _Ready()
        {
            AuthoredObjectIds.UnionWith(Descendants().OfType<ILocationStateParticipant>().Select(x => x.ObjectId));
            // F6 runs the location without Main's coordinator.
            if (GetParent() is not Window) return;
            IsPreparing = false;
            foreach (var point in Descendants().OfType<Core.Entity.IPersistentSpawnPoint>().ToList()) point.FillFresh();
        }

        /// <summary>The location the node belongs to: the node itself or its nearest location ancestor; null outside every location.</summary>
        public static LocationRoot? Find(Node node) => node.FindSelfOrAncestor<LocationRoot>();

        public IEnumerable<Node> Descendants() => Walk(this);
        private static IEnumerable<Node> Walk(Node node)
        {
            for (int i = 0; i < node.GetChildCount(); i++)
            {
                var child = node.GetChild(i);
                yield return child;
                foreach (var descendant in Walk(child)) yield return descendant;
            }
        }

        public LocationEndpoint Endpoint(string id) => Descendants().OfType<LocationEndpoint>().Single(x => x.EndpointId == id);
    }
}
