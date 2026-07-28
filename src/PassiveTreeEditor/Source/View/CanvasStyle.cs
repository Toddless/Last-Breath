namespace PassiveTreeEditor.Source.View
{
    using Core.Enums;
    using Godot;
    using Model;

    /// <summary>
    /// The visual vocabulary of the canvas, ported from the Umbral passive-tree mockup: shape and
    /// size say what a node is, hue says which ray it belongs to, fill-versus-outline says whether it
    /// is allocated. Sizes are the mockup's own, so a tree laid out on its ring radii (R0 70 … R3 520)
    /// looks exactly like the design.
    /// </summary>
    public static class CanvasStyle
    {
        // ── palette ────────────────────────────────────────────────────────────────────────────

        public static readonly Color Void = new(0.043f, 0.035f, 0.024f);
        public static readonly Color Gold1 = new(0.941f, 0.831f, 0.537f);
        public static readonly Color Gold2 = new(0.847f, 0.706f, 0.369f);
        public static readonly Color Gold3 = new(0.639f, 0.510f, 0.247f);
        public static readonly Color Ink1 = new(0.945f, 0.906f, 0.827f);
        public static readonly Color Ink2 = new(0.757f, 0.702f, 0.576f);
        public static readonly Color Ink3 = new(0.541f, 0.490f, 0.388f);
        public static readonly Color GoldInk = new(0.478f, 0.384f, 0.192f);
        public static readonly Color TooltipBackground = new(0.063f, 0.047f, 0.027f, 0.97f);

        private static readonly Color s_nodeOffFill = new(0.086f, 0.067f, 0.039f);
        private static readonly Color s_keystoneOffFill = new(0.106f, 0.078f, 0.031f);
        private static readonly Color s_pathFill = new(0.165f, 0.129f, 0.063f);
        private static readonly Color s_onStroke = new(0.969f, 0.929f, 0.839f);
        private static readonly Color s_selectStroke = new(1f, 0.965f, 0.875f);

        public static readonly Color EdgeIdle = new(0.169f, 0.141f, 0.094f);
        public static readonly Color EdgePath = Gold1;
        public static readonly Color RingLine = new(0.227f, 0.188f, 0.125f);
        public static readonly Color RingLabel = new(0.298f, 0.259f, 0.192f);
        public static readonly Color CoreGlow = new(0.227f, 0.173f, 0.082f);
        public static readonly Color BoxSelect = new(0.941f, 0.831f, 0.537f, 0.10f);
        public static readonly Color BoxSelectBorder = new(0.941f, 0.831f, 0.537f, 0.55f);

        private static readonly Color s_strength = new(0.776f, 0.325f, 0.251f);
        private static readonly Color s_dexterity = new(0.435f, 0.620f, 0.333f);
        private static readonly Color s_intelligence = new(0.302f, 0.518f, 0.741f);

        // Wedge hues — the colour of a hybrid node is the colour of the wedge it sits on, so a
        // transition node reads as neither of the two rays it joins.
        private static readonly Color s_wedgeStrengthDexterity = new(0.690f, 0.518f, 0.196f);
        private static readonly Color s_wedgeDexterityIntelligence = new(0.184f, 0.561f, 0.541f);
        private static readonly Color s_wedgeIntelligenceStrength = new(0.541f, 0.388f, 0.678f);

        // ── wheel geometry (the draft's own numbers, drawn as layout guides) ───────────────────

        /// <summary>R0 core … R3 outer rim. Nodes are not snapped to these — they are guides.</summary>
        public static readonly float[] RingRadii = [70f, 215f, 385f, 520f];

        public const float SectorRadius = 548f;
        public const float CoreGlowRadius = 112f;

        /// <summary>Six 60° sectors: three stance rays alternating with three hybrid wedges, in the
        /// mockup's angles (screen space, degrees, 0 = right, growing clockwise).</summary>
        public static readonly (float Angle, Color Color, string Label)[] Sectors =
        [
            (-90f, s_strength, "Strength"),
            (-30f, s_wedgeIntelligenceStrength, "INT + STR"),
            (30f, s_intelligence, "Intelligence"),
            (90f, s_wedgeDexterityIntelligence, "DEX + INT"),
            (150f, s_dexterity, "Dexterity"),
            (210f, s_wedgeStrengthDexterity, "STR + DEX")
        ];

        public const float SectorHalfAngle = 30f;

        /// <summary>Below this zoom the canvas stops drawing text — the only per-node cost that is
        /// not constant, and the one that matters when the whole tree is on screen.</summary>
        public const float LabelZoomThreshold = 0.55f;

        // ── per-node look ──────────────────────────────────────────────────────────────────────

        public static float Radius(PassiveNodeKind kind) => kind switch
        {
            PassiveNodeKind.Small => 5.6f,
            PassiveNodeKind.Notable => 12f,
            PassiveNodeKind.Keystone => 17f,
            PassiveNodeKind.AbilityUnlock => 11f,
            PassiveNodeKind.SocketTier2 => 6f,
            PassiveNodeKind.SocketTier3 => 7.5f,
            PassiveNodeKind.Start => 14f,
            _ => 6f
        };

        /// <summary>Hue of the node: its ray, the wedge it bridges when it is a hybrid, or gold when
        /// it belongs to no ray at all (the neutral core).</summary>
        public static Color NodeColor(PassiveNode node) =>
            node.IsHybrid
                ? WedgeColor(node.Stance!.Value, node.HybridStance!.Value)
                : RayColor(node.Stance);

        public static Color RayColor(Stance? stance) => stance switch
        {
            Stance.Strength => s_strength,
            Stance.Dexterity => s_dexterity,
            Stance.Intelligence => s_intelligence,
            _ => Gold2
        };

        private static Color WedgeColor(Stance first, Stance second)
        {
            bool strength = first == Stance.Strength || second == Stance.Strength;
            bool dexterity = first == Stance.Dexterity || second == Stance.Dexterity;
            bool intelligence = first == Stance.Intelligence || second == Stance.Intelligence;

            if (strength && dexterity) return s_wedgeStrengthDexterity;
            if (dexterity && intelligence) return s_wedgeDexterityIntelligence;
            if (intelligence && strength) return s_wedgeIntelligenceStrength;

            return Gold2;
        }

        /// <summary>Fill, outline and outline width for a node in a given state — the direct port of
        /// the mockup's <c>.nd.off / .nd.on / .nd.inpath</c> rules.</summary>
        public static NodeLook Look(PassiveNodeKind kind, Color accent, NodeState state)
        {
            bool keystone = kind == PassiveNodeKind.Keystone;

            return state switch
            {
                NodeState.Taken when keystone => new NodeLook(Gold1, s_selectStroke, 2f),
                NodeState.Taken => new NodeLook(accent, new Color(s_onStroke, 0.9f), 1.6f),
                NodeState.OnPath => new NodeLook(s_pathFill, Gold1, 2.6f),
                _ when keystone => new NodeLook(s_keystoneOffFill, new Color(accent, 0.75f), 2.6f),
                _ => new NodeLook(s_nodeOffFill, new Color(accent, 0.45f), 2f)
            };
        }

        public static Color SelectionStroke => s_selectStroke;

        /// <summary>Only these classes carry a caption; small and socket nodes would just add noise.</summary>
        public static bool IsLabelled(PassiveNodeKind kind) => kind
            is PassiveNodeKind.Start
            or PassiveNodeKind.AbilityUnlock
            or PassiveNodeKind.Keystone
            or PassiveNodeKind.Notable;

        public static string ShortLabel(PassiveNode node)
        {
            if (!string.IsNullOrWhiteSpace(node.Title)) return node.Title;

            return node.Kind switch
            {
                PassiveNodeKind.AbilityUnlock or PassiveNodeKind.Start => node.AbilityId,
                PassiveNodeKind.SocketTier2 => $"T2 {node.AbilityId}",
                PassiveNodeKind.SocketTier3 => $"T3 {node.AbilityId}",
                _ => node.Id
            };
        }
    }

    public enum NodeState
    {
        Idle,
        Taken,
        OnPath
    }

    public readonly record struct NodeLook(Color Fill, Color Outline, float OutlineWidth);
}
