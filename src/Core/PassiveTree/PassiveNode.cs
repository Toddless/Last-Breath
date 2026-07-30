namespace Core.PassiveTree
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// A single point on the tree. Identity is <see cref="Id"/> — links, allocation and the save
    /// format all address a node by it, so a rename has to travel through the document.
    /// </summary>
    public sealed class PassiveNode
    {
        public string Id { get; set; } = string.Empty;

        public PassiveNodeKind Kind { get; set; } = PassiveNodeKind.Small;

        /// <summary>Which ray the node belongs to; null for the neutral core.</summary>
        public Stance? Stance { get; set; }

        /// <summary>
        /// The second ray of a hybrid node — the transition nodes sitting on a wedge between two rays,
        /// which give half to each stance instead of full to one. Null for everything on a single ray.
        /// </summary>
        public Stance? HybridStance { get; set; }

        public bool IsHybrid => Stance is not null && HybridStance is not null && Stance != HybridStance;

        public float X { get; set; }

        public float Y { get; set; }

        /// <summary>Display name (or a localization key once the game reads this file).</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Rule text for keystones, and a place to write down formula-shaped notables.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Ability this node unlocks or sockets into; empty for content nodes.</summary>
        public string AbilityId { get; set; } = string.Empty;

        public List<ModifierLine> Modifiers { get; } = [];
    }
}
