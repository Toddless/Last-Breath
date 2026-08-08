namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>
    /// One node of the wheel that is worth a scene of its own: the classes the look gives a scene to,
    /// plus every node the character has taken. Which nodes those are is <see cref="NodeCarriers"/>'
    /// answer, read once per allocation pass — the view itself decides nothing.
    ///
    /// It is presentation and nothing else. It holds no state anybody asks it for, it never answers
    /// what was clicked, and it is not a mouse target: hit testing is one rule on the canvas above,
    /// because around a hundred and fifty pairs of shapes in the authored layout physically overlap
    /// and the engine's own picking would resolve them by the order of lines in a file its author
    /// rewrites daily.
    ///
    /// Everything that will one day animate hangs here: the body scales, the glow carries the pulse
    /// shader, and the clips are named by exports so a clip is added to the scene without touching this
    /// file.
    /// </summary>
    [GlobalClass]
    public partial class PassiveNodeView : Node2D
    {
        /// <summary>The uid the scene file declares in its own header — the authority, so a copy that
        /// drifts from it resolves to nothing at all in the game project.</summary>
        private const string UID = "uid://dnx5txabkc2ef";

        /// <summary>Seeds the pulse so a hundred taken nodes do not breathe in unison, which reads as
        /// a bug rather than as life.</summary>
        private const string PhaseUniform = "phase";

        private static readonly Color s_dropAccepted = Colors.LimeGreen;
        private static readonly Color s_dropRefused = Colors.IndianRed;

        /// <summary>Classes already reported as carrying art too small for the deepest zoom. Said once
        /// per class rather than once per node: the wheel builds dozens of scenes of the same class.</summary>
        private static readonly HashSet<PassiveNodeKind> s_reportedBlur = [];

        [Export] private Sprite2D? _body;
        [Export] private Sprite2D? _glow;
        [Export] private Sprite2D? _socketMark;
        [Export] private Sprite2D? _dropMark;
        [Export] private AnimationPlayer? _fx;

        /// <summary>Clip names are data: a clip added to the scene is played by naming it here, and an
        /// empty name simply means the animation has not been made yet.</summary>
        [Export] private StringName? _takeClip;

        [Export] private StringName? _refundClip;
        [Export] private StringName? _installClip;

        private PassiveNodeVisual? _visual;
        private Action? _release;

        /// <summary>
        /// Locks every control under this node out of the engine's mouse pass. In code rather than only
        /// in the scene because it is a lock and not a precaution: a Control living under a Node2D under
        /// a Control is exactly the shape the GUI walk picks up, and somebody dropping a Button into the
        /// scene later must not be able to create a second picker.
        /// </summary>
        public override void _Ready()
        {
            IgnoreMouse(this);
            if (_fx != null) _fx.AnimationFinished += OnClipFinished;
        }

        public override void _ExitTree()
        {
            if (_fx != null) _fx.AnimationFinished -= OnClipFinished;
            ReleaseNow();
        }

        /// <summary>Which node this view stands for. Position included: the whole wheel is drawn in
        /// document coordinates inside one scaled frame, so a node sits at the coordinates its author
        /// typed and panning the wheel writes nothing into any view.</summary>
        public void SetNode(PassiveNode node, PassiveWheelStyle style)
        {
            _visual = style.Visual(node.Kind);
            Position = new Vector2(node.X, node.Y);

            Color accent = style.ColorOf(node);
            if (_body != null)
            {
                _body.Texture = _visual.Body;
                _body.SelfModulate = accent;
                Normalize(_body, _visual.Radius * 2f);
            }

            if (_glow != null)
            {
                _glow.Texture = _visual.Glow;
                _glow.SelfModulate = accent;
                Normalize(_glow, _visual.GlowRadius * 2f);
                SeedPulse(node.Id);
            }

            ReportBlur(style);
            SetState(PassiveNodeVisualState.Idle);
            SetDropTarget(null);
            SetSocketMark(null);
        }

        /// <summary>
        /// Taken, planned, on the previewed route, or none of those. Only what the character actually
        /// HOLDS is drawn here: the glow is the pulse's carrier and is lit by nothing else, so "every
        /// taken node breathes" needs no list, no timer and no subscription of its own — and a node the
        /// plan only MEANS to buy does not breathe, because it has not been bought.
        /// <para>What a MARKED node looks like is deliberately not here. Most of the road is made of the
        /// class drawn as part of the mass, which has no scene to paint, so the plan's marks are drawn
        /// for every node at once by <see cref="WheelCursorLayer"/> — a second reading of the rule in
        /// here would be the one way the same mark comes out two different ways.</para>
        /// </summary>
        public void SetState(PassiveNodeVisualState state)
        {
            if (_body != null && _visual != null)
            {
                _body.Texture = state == PassiveNodeVisualState.Taken && _visual.BodyTaken != null
                    ? _visual.BodyTaken
                    : _visual.Body;
                Normalize(_body, _visual.Radius * 2f);
            }

            if (_glow != null) _glow.Visible = state == PassiveNodeVisualState.Taken;
        }

        /// <summary>Null when no drag is in the air; otherwise whether what is being carried would go
        /// into this node's slot. The verdict comes from the gate the drop itself goes through, so the
        /// mark can never promise what the bus then refuses.</summary>
        public void SetDropTarget(bool? accepted)
        {
            if (_dropMark == null) return;

            _dropMark.Visible = accepted != null;
            _dropMark.SelfModulate = accepted == true ? s_dropAccepted : s_dropRefused;
        }

        public void SetSocketMark(Texture2D? mark)
        {
            if (_socketMark == null) return;

            _socketMark.Texture = mark;
            _socketMark.Visible = mark != null;
        }

        /// <summary>
        /// The one write a zoom step makes into a view, and the only thing a view is told about the
        /// view transform at all — a pan never reaches here. <paramref name="bodyScale"/> is the
        /// counter-scale that keeps a node its floored size on screen: it follows the class and the
        /// zoom and nothing else, so every node of a class is handed the same number, though the canvas
        /// walks its views and writes it into each of them.
        /// </summary>
        public void ApplyZoom(float bodyScale) => Scale = new Vector2(bodyScale, bodyScale);

        public void PlayTake() => Play(_takeClip);

        public void PlayInstall() => Play(_installClip);

        /// <summary>
        /// Plays the give-back clip and calls back when it is over. The callback is what returns the
        /// view to the pool, so the node stays a carrier for the length of the clip — releasing it up
        /// front would let the layer that draws the mass claim the node while its scene is still on
        /// screen, and the node would be drawn twice for as long as the animation lasts.
        /// </summary>
        public void PlayRefundThen(Action release)
        {
            _release = release;
            if (Play(_refundClip)) return;

            ReleaseNow();
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        /// <summary>
        /// Makes the sprite cover exactly <paramref name="documentDiameter"/> document units, whatever
        /// the texture measures. The one place a texture size turns into a scale: the art is authored
        /// several times larger than the node so it stays sharp at the deepest zoom, and normalising by
        /// the CLASS instead of by the file would draw that art several times too big.
        /// </summary>
        private static void Normalize(Sprite2D sprite, float documentDiameter)
        {
            int width = sprite.Texture?.GetWidth() ?? 0;
            float scale = width <= 0 ? 1f : documentDiameter / width;
            sprite.Scale = new Vector2(scale, scale);
        }

        /// <summary>Says out loud that a class carries art too small for the deepest zoom — the one
        /// symptom nobody reports as a bug, because a blurred dot still looks like a dot. Whoever brings
        /// real art hears it even if he never reads the rule beside the export.</summary>
        private void ReportBlur(PassiveWheelStyle style)
        {
            if (_visual == null || _body?.Texture is not { } texture) return;

            float wanted = _visual.Radius * 2f * style.TextureOversample;
            if (texture.GetWidth() >= wanted || !s_reportedBlur.Add(_visual.Kind)) return;

            Tracker.TrackNotFound(
                $"Passive node art for '{_visual.Kind}' is {texture.GetWidth()}px wide and the wheel stretches it to " +
                $"{wanted:0}px at full zoom — author it at 2 x Radius x TextureOversample", this);
        }

        private bool Play(StringName? clip)
        {
            if (_fx == null || clip == null || clip.IsEmpty || !_fx.HasAnimation(clip)) return false;

            _fx.Play(clip);
            return true;
        }

        private void OnClipFinished(StringName clip) => ReleaseNow();

        private void ReleaseNow()
        {
            Action? release = _release;
            _release = null;
            release?.Invoke();
        }

        /// <summary>A stable seed per node, so the same node always breathes the same and two nodes
        /// beside each other do not. Written out rather than taken from the string hash, which is
        /// salted per process and would reshuffle the wheel on every launch.</summary>
        private void SeedPulse(string nodeId)
        {
            if (_glow?.Material is not ShaderMaterial material) return;

            uint hash = 2166136261u;
            foreach (char letter in nodeId)
            {
                hash ^= letter;
                hash *= 16777619u;
            }

            material.SetShaderParameter(PhaseUniform, hash % 1000u / 1000f);
        }

        private static void IgnoreMouse(Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                if (child is Control control) control.MouseFilter = Control.MouseFilterEnum.Ignore;
                IgnoreMouse(child);
            }
        }
    }
}
