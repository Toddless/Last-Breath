namespace LastBreath.World.Interactions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.World.Interactions;
    using Godot;
    using Locations;

    [GlobalClass]
    public partial class InteractionTarget : Area2D
    {
        public const uint DetectionLayer = 1u << 15;
        [Export] public string ObjectId { get; set; } = "";
        [Export] public float Reach { get; set; } = 150;
        [Export(PropertyHint.Layers2DPhysics)] public uint ObstacleMask { get; set; } = InteractionReach.BlockerMask;
        private bool _registered;
        protected virtual string StableObjectId => ObjectId;
        public virtual bool IsAvailable => true;
        public InteractionHandle Handle { get; private set; }
        public Node2D Anchor => GetNodeOrNull<Node2D>("HintAnchor") ?? this;
        public IEnumerable<IInteractionSource> Sources
        {
            get
            {
                if (GetParent() is IInteractionSource source) yield return source;
                for (int i = 0; i < GetChildCount(); i++)
                    if (GetChild(i) is IInteractionSource child) yield return child;
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
            var root = LocationRoot.Find(this);
            if (root == null) return;
            Handle = new(root.LocationId, StableObjectId, Guid.NewGuid());
            Services.GameServiceProvider.Instance.GetService<InteractionService>().Register(this);
            _registered = true;
        }

        public override void _Ready()
        {
            if (string.IsNullOrWhiteSpace(StableObjectId) || !float.IsFinite(Reach) || Reach <= 0)
                throw new InvalidOperationException("Interaction target needs a stable ID and a positive reach.");
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

        public IReadOnlyList<InteractionAction> ReadActions()
        {
            if (!IsAvailable) return [];
            var actions = Sources.SelectMany(x => x.Actions()).ToList();
            if (actions.Select(x => x.Id).Distinct().Count() != actions.Count)
                throw new InvalidOperationException($"Duplicate action ID on '{ObjectId}'.");
            return actions;
        }
    }
}
