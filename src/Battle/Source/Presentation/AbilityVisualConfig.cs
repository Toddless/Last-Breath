namespace Battle.Source.Presentation
{
    using Godot;

    /// <summary>How the ability's visuals are delivered on screen. Presentation-only concept.</summary>
    public enum VfxDeliveryKind
    {
        /// <summary>Cast pose only, no extra visuals (attack-series abilities animate through attack beats).</summary>
        None,

        /// <summary>The travel clip flies from the caster to every hit target; the impact clip lands with the hit.</summary>
        Projectile,

        /// <summary>Hits play in recorded order; the travel clip flies from the previous target to the next one.</summary>
        Chain,

        /// <summary>No flight — the impact clip appears right on the target (falling block, eruption).</summary>
        InstantOnTarget,

        /// <summary>Self-cast: the travel clip plays as an aura on the caster.</summary>
        SelfAura
    }

    /// <summary>
    /// Per-ability visual descriptor: what flies, what lands and how fast. Clips are animation names
    /// inside the library's SpriteFrames; an empty clip name skips that stage — placeholders can be
    /// added in the editor without touching code.
    /// </summary>
    [GlobalClass]
    public partial class AbilityVisualConfig : Resource
    {
        [Export] public string AbilityId { get; set; } = string.Empty;
        [Export] public VfxDeliveryKind Delivery { get; set; } = VfxDeliveryKind.None;

        /// <summary>Activation VFX on the caster, played TOGETHER with the cast pose (any delivery kind).</summary>
        [Export] public string CastClip { get; set; } = string.Empty;

        /// <summary>Flight clip (Projectile/Chain) or the aura clip (SelfAura).</summary>
        [Export] public string TravelClip { get; set; } = string.Empty;

        /// <summary>Clip played on the target when the hit lands.</summary>
        [Export] public string ImpactClip { get; set; } = string.Empty;

        /// <summary>Flight speed in pixels per second.</summary>
        [Export] public float TravelSpeed { get; set; } = 1200f;

        [Export] public float Scale { get; set; } = 1f;

        /// <summary>Play the cast clip anchored above the arena (not on the caster) — e.g. a storm cloud
        /// summoned over the whole field that then strikes the targets.</summary>
        [Export] public bool CastOverArena { get; set; }

        /// <summary>Scale for the over-arena cast clip, independent of <see cref="Scale"/> (which drives the
        /// projectile/impact clips) so the cloud can be big without enlarging the strikes.</summary>
        [Export] public float CastScale { get; set; } = 1f;

        /// <summary>Melee series: the caster runs up to the target and the hits land at arm's reach.
        /// Basic attacks approach always — this flag exists for ability casts only.</summary>
        [Export] public bool MeleeApproach { get; set; }
    }
}
