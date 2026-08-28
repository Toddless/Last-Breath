namespace Core.Entity.Silhouette
{
    using System;

    /// <summary>
    /// Fits the NPC scene's collision shapes to the art that actually plays on the sprite. The
    /// scenes were authored against the humanoid placeholder; every shape is re-expressed as
    /// proportions of that placeholder's silhouette (widths as shares of body width, the shape's
    /// bottom edge by its distance to the feet as a share of body height) and those proportions
    /// are applied to the new art's silhouette. All outputs are NPC-local coordinates, ready to
    /// go onto CollisionShape2D nodes.
    ///
    /// V1 limitation (deliberate): one profile per NPC, measured on the first Idle_Down frame.
    /// Side frames of four-legged art are wider than the front frame, so side-on the fit is a
    /// little narrow; per-facing profiles are a later step. Frames are assumed 480x480 with the
    /// sprite centered — the library convention (NpcVisualConfig).
    /// </summary>
    public sealed class NpcSilhouetteCollisionCalculator
    {
        /// <summary>Frames are 480x480 and the sprite is centered, so frame pixel (240,240) is NPC-local (0,0).</summary>
        private const float FrameCenter = 240f;

        /// <summary>
        /// Used rect of the humanoid placeholder's first Idle_Down frame — measured off the real
        /// texture (Main/Npc/Front - Idle.png, frame region 0,0,480,480; alpha bounding box).
        /// The invariant-pin test feeds this rect back in and must get the authored scene numbers
        /// out unchanged, otherwise every humanoid NPC shifts.
        /// </summary>
        public static readonly SilhouetteRect HumanoidReferenceRect = new(152f, 86f, 177f, 297f);

        /// <summary>Main scene (Main/Npc/TestNpc.tscn): sprite base scale 1, capsule body, dialogue circle.
        /// The layouts are the scene's authored numbers in NPC-local coordinates (node + shape offsets merged).</summary>
        public static NpcSilhouetteCollisionCalculator Main { get; } = new(
            referenceScaleX: 1f,
            referenceScaleY: 1f,
            authored: new NpcCollisionProfile(
                BodyCapsule: new CapsuleLayout(0f, 92f, 37.4f, 136.39f),
                BodyCircle: null,
                Interaction: new CapsuleLayout(0f, 44f, 35f, 239.69f), // Area2D (0,101) + shape (0,-57)
                Dialogue: new CircleLayout(0f, 85f, 77.64f))); // DialogueActor (0,70) + shape (0,15)

        /// <summary>Battle scene (Battle/Internal/Npc/TestNpc.tscn): sprite base scale 0.35, circle body, no dialogue.</summary>
        public static NpcSilhouetteCollisionCalculator Battle { get; } = new(
            referenceScaleX: 0.35f,
            referenceScaleY: 0.35f,
            authored: new NpcCollisionProfile(
                BodyCapsule: null,
                BodyCircle: new CircleLayout(0f, 0f, 32f),
                Interaction: new CapsuleLayout(0f, 0f, 35f, 120f),
                Dialogue: null));

        private readonly NpcCollisionProfile _authored;
        private readonly LocalRect _reference;

        private NpcSilhouetteCollisionCalculator(float referenceScaleX, float referenceScaleY, NpcCollisionProfile authored)
        {
            _authored = authored;
            _reference = Localize(HumanoidReferenceRect, referenceScaleX, referenceScaleY);
        }

        /// <summary>
        /// The fit for one NPC: the art's silhouette (used rect in frame pixels) plus the sprite's
        /// FINAL scale (scene base scale already multiplied by the visual config's scale). A
        /// degenerate silhouette or scale returns the authored scene layout untouched — the safe
        /// fallback that moves nobody.
        /// </summary>
        public NpcCollisionProfile Calculate(SilhouetteRect silhouette, float scaleX, float scaleY)
        {
            if (!silhouette.IsUsable || scaleX <= 0 || scaleY <= 0) return _authored;

            var target = Localize(silhouette, scaleX, scaleY);
            float widthRatio = target.Width / _reference.Width;
            float heightRatio = target.Height / _reference.Height;

            return new NpcCollisionProfile(
                _authored.BodyCapsule is { } bodyCapsule ? MapCapsule(bodyCapsule, target, widthRatio, heightRatio) : null,
                _authored.BodyCircle is { } bodyCircle ? MapCircle(bodyCircle, target, widthRatio, heightRatio) : null,
                MapCapsule(_authored.Interaction, target, widthRatio, heightRatio),
                _authored.Dialogue is { } dialogue ? MapCircle(dialogue, target, widthRatio, heightRatio) : null);
        }

        /// <summary>Radii/widths follow body width, vertical size follows body height, and the
        /// shape's bottom edge keeps its authored distance to the feet (scaled by height). When
        /// the width-driven radius forces the capsule taller than the height-driven size (a wide
        /// low animal — a capsule cannot be wider than tall), the extra height grows upward so
        /// the bottom edge stays anchored to the feet.</summary>
        private CapsuleLayout MapCapsule(in CapsuleLayout authored, in LocalRect target, float widthRatio, float heightRatio)
        {
            float radius = authored.Radius * widthRatio;
            float scaledHeight = authored.Height * heightRatio;
            float height = Math.Max(scaledHeight, 2f * radius);
            float centerX = target.CenterX + (authored.CenterX - _reference.CenterX) * widthRatio;
            float centerY = target.Bottom - (_reference.Bottom - authored.CenterY) * heightRatio - (height - scaledHeight) / 2f;
            return new CapsuleLayout(centerX, centerY, radius, height);
        }

        /// <summary>Same anchoring for circles: the radius follows width, and the center shifts
        /// so the bottom edge stays where the height ratio alone would have put it.</summary>
        private CircleLayout MapCircle(in CircleLayout authored, in LocalRect target, float widthRatio, float heightRatio)
        {
            float radius = authored.Radius * widthRatio;
            float centerX = target.CenterX + (authored.CenterX - _reference.CenterX) * widthRatio;
            float centerY = target.Bottom - (_reference.Bottom - authored.CenterY) * heightRatio - (radius - authored.Radius * heightRatio);
            return new CircleLayout(centerX, centerY, radius);
        }

        /// <summary>A silhouette translated into NPC-local coordinates at a given sprite scale.</summary>
        private static LocalRect Localize(SilhouetteRect rect, float scaleX, float scaleY) => new(
            Width: rect.Width * scaleX,
            Height: rect.Height * scaleY,
            Bottom: (rect.Bottom - FrameCenter) * scaleY,
            CenterX: (rect.CenterX - FrameCenter) * scaleX);

        private readonly record struct LocalRect(float Width, float Height, float Bottom, float CenterX);
    }
}
