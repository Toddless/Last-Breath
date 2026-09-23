namespace LastBreath.World.Interactions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Core;
    using Core.World.Interactions;
    using Godot;
    using Locations;

    [GlobalClass]
    public partial class InteractionTarget : Area2D
    {
        private const string InvalidConfigurationFormat =
            "Interaction target '{0}' is disabled: it needs a non-empty object ID and a positive finite reach, but has ID '{1}' and reach {2}";
        private const string DuplicateActionsFormat = "Interaction target '{0}' ('{1}') disables the actions that share an ID: {2}";
        private const string ActionIdSeparator = ", ";
        public const uint DetectionLayer = 1u << 15;
        [Export] public string ObjectId { get; set; } = "";
        [Export] public float Reach { get; set; } = 150;
        [Export(PropertyHint.Layers2DPhysics)] public uint ObstacleMask { get; set; } = InteractionReach.BlockerMask;
        /// <summary>Bodies of this object that do not block access to its own interaction points.</summary>
        [Export] public Godot.Collections.Array<CollisionObject2D> OwnBodies { get; set; } = [];
        private readonly HashSet<string> _reportedDuplicateActionIds = [];
        private bool _registered;
        private bool _configurationReported;
        protected virtual string StableObjectId => ObjectId;
        public virtual bool IsAvailable => true;
        public InteractionHandle Handle { get; private set; }
        public Node2D Anchor => GetNodeOrNull<Node2D>("HintAnchor") ?? this;
        /// <summary>The owner when it is a source, then every source among the descendants in tree order; a nested target keeps its own.</summary>
        public IEnumerable<IInteractionSource> Sources
        {
            get
            {
                if (GetParent() is IInteractionSource owner) yield return owner;
                foreach (var source in SourcesUnder(this)) yield return source;
            }
        }
        public IEnumerable<Vector2> Points
        {
            get
            {
                bool found = false;
                for (int i = 0; i < GetChildCount(); i++)
                {
                    if (GetChild(i) is not Marker2D point || !point.Name.ToString().StartsWith("InteractionPoint", StringComparison.Ordinal)) continue;
                    found = true;
                    yield return point.GlobalPosition;
                }
                if (!found) yield return GlobalPosition;
            }
        }

        public override void _EnterTree()
        {
            if (!CheckConfiguration() || LocationRoot.Find(this) is not { } root) return;
            Handle = new(root.LocationId, StableObjectId, Guid.NewGuid());
            _registered = Services.GameServiceProvider.Instance.GetService<InteractionService>().TryRegister(this);
        }

        public override void _Ready()
        {
            if (!CheckConfiguration()) return;
            CollisionLayer = DetectionLayer;
            CollisionMask = 0;
            Monitoring = false;
            InputPickable = false;
            // Conservative discovery shape; exact reach is checked at each authored point.
            float extent = Points.Max(p => GlobalPosition.DistanceTo(p)) + Reach;
            float scale = Math.Max(0.001f, Math.Min(Math.Abs(GlobalScale.X), Math.Abs(GlobalScale.Y)));
            AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = extent / scale } });
        }

        public override void _ExitTree()
        {
            if (_registered) Services.GameServiceProvider.Instance.GetService<InteractionService>().Unregister(this);
            _registered = false;
        }

        /// <summary>Actions offered now: none while the target is unregistered or unavailable; actions that share an ID are left out.</summary>
        public IReadOnlyList<InteractionAction> ReadActions() => ReadOffers().ConvertAll(x => x.Action);

        /// <summary>Source of the enabled action with this ID among the offered actions; null when <see cref="ReadActions"/> does not offer it.</summary>
        public IInteractionSource? FindEnabledSource(string actionId) =>
            ReadOffers().Where(x => x.Action.Enabled && x.Action.Id == actionId).Select(x => x.Source).FirstOrDefault();

        /// <summary>True for a non-empty stable ID and a positive finite reach; an invalid setup is reported once per node.</summary>
        private bool CheckConfiguration()
        {
            if (!string.IsNullOrWhiteSpace(StableObjectId) && float.IsFinite(Reach) && Reach > 0) return true;
            if (_configurationReported) return false;
            _configurationReported = true;
            Tracker.TrackError(string.Format(CultureInfo.InvariantCulture, InvalidConfigurationFormat, GetPath(), StableObjectId, Reach), this);
            return false;
        }

        /// <summary>Sources among the node's descendants in tree order; the subtree of a nested target is left to that target.</summary>
        private static IEnumerable<IInteractionSource> SourcesUnder(Node node)
        {
            for (int i = 0; i < node.GetChildCount(); i++)
            {
                var child = node.GetChild(i);
                if (child is InteractionTarget) continue;
                if (child is IInteractionSource source) yield return source;
                // A leaf has nothing below it: skipping it spares an enumerator per leaf on every read.
                if (child.GetChildCount() == 0) continue;
                foreach (var descendant in SourcesUnder(child)) yield return descendant;
            }
        }

        private List<ActionOffer> ReadOffers()
        {
            if (!_registered || !IsAvailable) return [];
            var offers = Sources.SelectMany(source => source.Actions().Select(action => new ActionOffer(source, action))).ToList();
            var duplicates = DuplicateActionIds(offers);
            if (duplicates.Count == 0) return offers;
            ReportDuplicateActions(duplicates);
            offers.RemoveAll(x => duplicates.Contains(x.Action.Id));
            return offers;
        }

        private static HashSet<string> DuplicateActionIds(IEnumerable<ActionOffer> offers) =>
            offers.GroupBy(x => x.Action.Id).Where(x => x.Skip(1).Any()).Select(x => x.Key).ToHashSet();

        /// <summary>Reports only IDs not reported before, so repeated reads of the same broken set stay silent.</summary>
        private void ReportDuplicateActions(HashSet<string> duplicates)
        {
            if (duplicates.IsSubsetOf(_reportedDuplicateActionIds)) return;
            _reportedDuplicateActionIds.UnionWith(duplicates);
            Tracker.TrackError(string.Format(DuplicateActionsFormat, GetPath(), Handle.ObjectId,
                string.Join(ActionIdSeparator, duplicates.Order(StringComparer.Ordinal))), this);
        }

        private readonly record struct ActionOffer(IInteractionSource Source, InteractionAction Action);
    }
}
