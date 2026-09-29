namespace LastBreath.World.Interactions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Core;
    using Core.Views.UI;
    using Core.World.Interactions;
    using Godot;
    using Locations;

    [GlobalClass]
    public partial class InteractionTarget : Area2D
    {
        private const string InvalidConfigurationFormat =
            "Interaction target '{0}' is disabled: it needs a non-empty object ID and a positive finite reach, but has ID '{1}' and reach {2}";
        private const string DuplicateActionsFormat = "Interaction target '{0}' ('{1}') disables the actions that share an ID: {2}";
        private const string OverriddenObjectIdFormat = "Interaction target '{0}' ignores its ObjectId '{1}': its owner gives the ID '{2}'";
        private const string MissingAnchorFormat =
            "Interaction target '{0}' shows its hint at its own position: it needs its hint anchor set in the scene";
        private const string InvalidDiscoveryFormat =
            "Interaction target '{0}' is disabled: {1}; it needs a direct child CollisionShape2D with a CircleShape2D centred on it and a radius of at least {2:0.##}";
        private const string MissingShapeProblem = "its discovery shape is not set";
        private const string ForeignShapeProblem = "its discovery shape is not its direct child";
        private const string DisabledShapeProblem = "its discovery shape is disabled";
        private const string NotCircleProblem = "its discovery shape holds no CircleShape2D";
        private const string OffCentreProblem = "its discovery circle is not centred on it";
        private const string SmallRadiusFormat = "its discovery circle has a radius of {0:0.##}";
        private const string ActionIdSeparator = ", ";
        /// <summary>Slack for the float rounding of global positions when the authored radius is compared with the required one.</summary>
        private const float RadiusTolerance = 0.01f;
        /// <summary>Floor of the scale the required radius is divided by, so a collapsed node cannot divide by zero.</summary>
        private const float MinimumScale = 0.001f;
        public const uint DetectionLayer = 1u << 15;
        [Export] public string ObjectId { get; set; } = "";
        [Export] public float Reach { get; set; } = 150;
        [Export(PropertyHint.Layers2DPhysics)] public uint ObstacleMask { get; set; } = InteractionReach.BlockerMask;
        /// <summary>Bodies of this object that do not block access to its own interaction points.</summary>
        [Export] public Godot.Collections.Array<CollisionObject2D> OwnBodies { get; set; } = [];

        /// <summary>Points the player's reach is measured to; while none is set, the target's own position stands in.</summary>
        [Export] private Godot.Collections.Array<Marker2D> InteractionPoints { get; set; } = [];

        /// <summary>Node the hint and the menu stand at; while it is not set, the target itself stands in and the gap is reported once.</summary>
        [Export] private Node2D? HintAnchor { get; set; }

        /// <summary>Circle the player's sensor finds the target by: a direct child centred on the target that covers every interaction
        /// point plus Reach; any other setup is reported once and disables the target.</summary>
        [Export] private CollisionShape2D? DiscoveryShape { get; set; }

        private readonly HashSet<string> _reportedDuplicateActionIds = [];
        private bool _registered;
        private bool _configurationReported;
        private bool _overrideReported;
        private bool _anchorReported;
        /// <summary>Whether the discovery shape failed its check; the target then stays unregistered whenever it enters the tree.</summary>
        private bool _discoveryInvalid;
        /// <summary>Owner found when the target last entered the tree; null while it has none.</summary>
        private IInteractionOwner? _interactionOwner;
        /// <summary>Actions the sources gave at the last read; null until the next read after <see cref="InvalidateOffer"/>.</summary>
        private IReadOnlyList<InteractionAction>? _offer;
        /// <summary>Positions <see cref="ReadPoints"/> writes, reused by every read: one slot per point slot plus the target's own position.</summary>
        private Vector2[] _pointPositions = [];
        /// <summary>Registered and available: only such a target offers actions.</summary>
        private bool CanOffer => _registered && IsAvailable;
        /// <summary>ID the target registers under: its owner's ID while it has an owner, else its own <see cref="ObjectId"/>.</summary>
        protected virtual string StableObjectId => _interactionOwner?.InteractionId ?? ObjectId;
        /// <summary>Whether the target can be interacted with now: its owner's answer while it has an owner, else true.</summary>
        public virtual bool IsAvailable => _interactionOwner?.IsInteractable ?? true;
        public InteractionHandle Handle { get; private set; }
        /// <summary>The set hint anchor, or the target itself while none is set.</summary>
        public Node2D Anchor => ValidOrNull(HintAnchor) ?? this;
        /// <summary>The parent when it is a source, then every source among the descendants in tree order; a nested target keeps its own.</summary>
        public IEnumerable<IInteractionSource> Sources
        {
            get
            {
                if (GetParent() is IInteractionSource parent) yield return parent;
                foreach (var source in SourcesUnder(this)) yield return source;
            }
        }
        /// <summary>Actions kept from the last read of the sources, read again on first use after <see cref="InvalidateOffer"/>; none while
        /// the target is unregistered or unavailable, which also drops the kept ones.</summary>
        public IReadOnlyList<InteractionAction> CachedOffer
        {
            get
            {
                if (!CanOffer) InvalidateOffer();
                else if (_offer == null) ReadOffers();
                return _offer ?? [];
            }
        }

        public override void _EnterTree()
        {
            _interactionOwner = FindInteractionOwner();
            ReportOverriddenObjectId();
            if (_discoveryInvalid || !CheckConfiguration() || LocationRoot.Find(this) is not { } root) return;
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
            CheckAnchor();
            CheckDiscovery();
        }

        public override void _ExitTree() => Unregister();

        /// <summary>Actions offered now, read from the sources and kept as <see cref="CachedOffer"/>: none while the target is unregistered or
        /// unavailable; actions that share an ID are left out.</summary>
        public IReadOnlyList<InteractionAction> ReadActions() => ReadOffers().ConvertAll(x => x.Action);

        /// <summary>Drops the kept offer, so the next use of <see cref="CachedOffer"/> reads the sources again.</summary>
        public void InvalidateOffer() => _offer = null;

        /// <summary>Global positions of the set interaction points, or the target's own position while none is set; the next read overwrites them.</summary>
        public ReadOnlySpan<Vector2> ReadPoints()
        {
            var points = InteractionPoints;
            int slots = points.Count;
            if (_pointPositions.Length <= slots) _pointPositions = new Vector2[slots + 1];
            int count = 0;
            for (int i = 0; i < slots; i++)
                if (ValidOrNull(points[i]) is { } point) _pointPositions[count++] = point.GlobalPosition;
            if (count == 0) _pointPositions[count++] = GlobalPosition;
            return _pointPositions.AsSpan(0, count);
        }

        /// <summary>Source of the enabled action with this ID among the offered actions; null when <see cref="ReadActions"/> does not offer it.</summary>
        public IInteractionSource? FindEnabledSource(string actionId) =>
            ReadOffers().Where(x => x.Action.Enabled && x.Action.Id == actionId).Select(x => x.Source).FirstOrDefault();

        /// <summary>The nearest ancestor implementing <see cref="IInteractionOwner"/>, starting at the parent; null when none does.</summary>
        public IInteractionOwner? FindInteractionOwner() => GetParent()?.FindSelfOrAncestor<IInteractionOwner>();

        /// <summary>True for a non-empty stable ID and a positive finite reach; an invalid setup is reported once per node.</summary>
        private bool CheckConfiguration()
        {
            if (!string.IsNullOrWhiteSpace(StableObjectId) && float.IsFinite(Reach) && Reach > 0) return true;
            if (_configurationReported) return false;
            _configurationReported = true;
            Tracker.TrackError(string.Format(CultureInfo.InvariantCulture, InvalidConfigurationFormat, GetPath(), StableObjectId, Reach), this);
            return false;
        }

        /// <summary>Reports once a scene-set ObjectId that the owner's ID replaces.</summary>
        private void ReportOverriddenObjectId()
        {
            if (_overrideReported || _interactionOwner == null || string.IsNullOrWhiteSpace(ObjectId)) return;
            _overrideReported = true;
            Tracker.TrackError(string.Format(OverriddenObjectIdFormat, GetPath(), ObjectId, _interactionOwner.InteractionId), this);
        }

        /// <summary>Reports once a target without its hint anchor; the target stays enabled and shows its hint at its own position.</summary>
        private void CheckAnchor()
        {
            if (_anchorReported || ValidOrNull(HintAnchor) != null) return;
            _anchorReported = true;
            Tracker.TrackError(string.Format(MissingAnchorFormat, GetPath()), this);
        }

        /// <summary>Disables the target with one report when its discovery shape does not cover every interaction point plus Reach.</summary>
        private void CheckDiscovery()
        {
            if (_discoveryInvalid) return;
            var shape = ValidOrNull(DiscoveryShape);
            float required = RequiredRadius(shape ?? (Node2D)this);
            if (FindDiscoveryProblem(shape, required) is not { } problem) return;
            _discoveryInvalid = true;
            Tracker.TrackError(string.Format(CultureInfo.InvariantCulture, InvalidDiscoveryFormat, GetPath(), problem, required), this);
            Unregister();
        }

        /// <summary>Why the shape cannot find the player wherever the target is in reach, for the report; null when it can.</summary>
        private string? FindDiscoveryProblem(CollisionShape2D? shape, float requiredRadius)
        {
            if (shape == null) return MissingShapeProblem;
            if (shape.GetParent() != this) return ForeignShapeProblem;
            if (shape.Disabled) return DisabledShapeProblem;
            if (shape.Shape is not CircleShape2D circle) return NotCircleProblem;
            if (!shape.Position.IsZeroApprox()) return OffCentreProblem;
            return circle.Radius + RadiusTolerance < requiredRadius
                ? string.Format(CultureInfo.InvariantCulture, SmallRadiusFormat, circle.Radius) : null;
        }

        /// <summary>Radius a circle drawn in the node's space needs to cover every interaction point plus Reach around the target.</summary>
        private float RequiredRadius(Node2D space) => (FarthestPointDistance() + Reach) / SmallerScale(space);

        /// <summary>Distance from the target to its farthest interaction point.</summary>
        private float FarthestPointDistance()
        {
            var origin = GlobalPosition;
            float farthest = 0;
            foreach (var point in ReadPoints()) farthest = Math.Max(farthest, origin.DistanceTo(point));
            return farthest;
        }

        /// <summary>The node's smaller absolute global scale, floored at <see cref="MinimumScale"/>.</summary>
        private static float SmallerScale(Node2D node) =>
            Math.Max(MinimumScale, Math.Min(Math.Abs(node.GlobalScale.X), Math.Abs(node.GlobalScale.Y)));

        /// <summary>Drops the target's registration, if any, and its kept offer.</summary>
        private void Unregister()
        {
            if (_registered) Services.GameServiceProvider.Instance.GetService<InteractionService>().Unregister(this);
            _registered = false;
            InvalidateOffer();
        }

        /// <summary>The object while it is set and not freed; null otherwise.</summary>
        private static T? ValidOrNull<T>(T? instance) where T : GodotObject =>
            instance != null && IsInstanceValid(instance) ? instance : null;

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

        /// <summary>Offers read from the sources now, whose actions become the kept offer; none, and nothing kept, while the target cannot offer.</summary>
        private List<ActionOffer> ReadOffers()
        {
            if (!CanOffer)
            {
                InvalidateOffer();
                return [];
            }
            var offers = CollectOffers();
            _offer = offers.ConvertAll(x => x.Action);
            return offers;
        }

        /// <summary>Offers of every source; actions that share an ID are reported and left out.</summary>
        private List<ActionOffer> CollectOffers()
        {
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
