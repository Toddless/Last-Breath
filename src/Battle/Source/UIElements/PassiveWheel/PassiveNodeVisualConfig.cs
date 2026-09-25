namespace Battle.Source.UIElements.PassiveWheel
{
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Godot;

    /// <summary>
    /// An authored look for one node of the wheel, or for a whole class of them. Every field here is an
    /// OVERRIDE of what <see cref="PassiveNodeVisual"/> already draws, and a field left empty overrides
    /// nothing — so a library without a row for a node draws the wheel that shipped before the channel
    /// existed. Art is added by writing rows in the editor, never by touching the drawing.
    /// <para><see cref="NodeId"/> alone decides which of the two layers a row belongs to: name a node and
    /// the row is that node's, leave it empty and the row is the default for <see cref="Kind"/>. A node's
    /// own row wins over its class's — see <see cref="PassiveNodeVisualIndex{TVisual}"/>.</para>
    /// <para>What the class table decides and this cannot: the SIZE of a node and whether the class is
    /// drawn as a scene at all. Both are read by the geometry the wheel picks clicks with, so they stay in
    /// one table (<see cref="PassiveWheelStyle"/>) rather than becoming per-node art.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveNodeVisualConfig : Resource, IPassiveNodeVisual
    {
        [Export] public string NodeId { get; set; } = string.Empty;

        [Export] public PassiveNodeKind Kind { get; set; }

        /// <summary>The node's face — the icon channel. Authored under the rule
        /// <see cref="PassiveNodeVisual.Body"/> is authored under, since the sprite is normalised to the
        /// node's document size by the same arithmetic.</summary>
        [Export] public Texture2D? Body { get; set; }

        /// <summary>The face of the node once the character holds it.</summary>
        [Export] public Texture2D? BodyTaken { get; set; }

        /// <summary>The halo a held node breathes with.</summary>
        [Export] public Texture2D? Glow { get; set; }

        /// <summary>Hue of the body, the halo and — for a node drawn in the mass — its outline. FULLY
        /// TRANSPARENT means unauthored, so raise the alpha or the colour is not read at all; see
        /// <see cref="ModulateOf"/> for what unauthored falls back to.</summary>
        [Export] public Color Tint { get; set; } = new(0f, 0f, 0f, 0f);

        /// <summary>Drawn in place of the filled dot on the ring of this node's ability — the picture of
        /// "an augment sits here". Slots that hold nothing keep their outline, so the ring still tells
        /// open from closed by shape and not by colour alone.</summary>
        [Export] public Texture2D? FilledSocketPip { get; set; }

        /// <summary>Takes away the dot beside a node whose own slot is occupied, leaving the node to say
        /// it through its ring alone. Off — the default — is the row saying nothing, the way a fully
        /// transparent <see cref="Tint"/> is: a row written to give one node an icon cannot switch a mark
        /// back on by accident.
        /// <para>Inherited no further than any other field: a row addressed to a node answers for ALL of
        /// them, so a node given its own row inside a class whose row hides the mark has to hide it here
        /// too.</para></summary>
        [Export] public bool HidesAugmentMark { get; set; }

        /// <summary>Ambient decoration instantiated under the node's own scene. Only nodes drawn as a
        /// scene carry one — a node drawn in the mass has nothing to hang it on.</summary>
        [Export] public PackedScene? Effect { get; set; }

        public bool HasTint => Tint.A > 0f;

        /// <summary>
        /// What a drawn piece of a node is modulated by. ONE reading for the two roads that draw a node —
        /// the pooled scene and the mass layer — so the same PNG cannot come out two colours depending on
        /// a flag (<see cref="PassiveNodeVisual.HasView"/>) that lives in another file and says nothing
        /// about colour.
        /// <para>The rule: art the LIBRARY authored keeps the colours it was drawn with and is touched
        /// only by a hue the same row authored; anything the CLASS draws — its ring body, its halo, the
        /// outline in the mass — goes on wearing the node's ray, which is the wheel as it shipped.</para>
        /// </summary>
        /// <param name="row">The node's row, or null when the library says nothing about it.</param>
        /// <param name="authored">Whether the piece being drawn is the row's own art. Asked of the texture
        /// actually chosen, never of the row as a whole: a row authoring only the taken face must not
        /// change how the idle one is painted.</param>
        /// <param name="ray">Hue of the node's ray — what the class's own art has always been painted with.</param>
        public static Color ModulateOf(PassiveNodeVisualConfig? row, bool authored, Color ray) =>
            authored ? row?.TintOr(Colors.White) ?? Colors.White : row?.TintOr(ray) ?? ray;

        /// <summary>The authored hue, or <paramref name="fallback"/> when the row leaves the palette
        /// alone. One reading, so the body, the halo and the outline cannot fall back differently.</summary>
        public Color TintOr(Color fallback) => HasTint ? Tint : fallback;
    }
}
