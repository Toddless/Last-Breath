namespace Core.PassiveTree.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;

    /// <summary>What one place of an ability's ring of slots is doing right now.</summary>
    public enum SocketSlotState
    {
        /// <summary>The opening node hasn't been bought. The board holds a socket exactly while its node
        /// is taken, so "no socket at that address" means "node not owned" — a merely-marked node never
        /// reaches the board.</summary>
        Unopened,

        /// <summary>Lit and free.</summary>
        Open,

        /// <summary>Lit and wearing an augment.</summary>
        Filled,

        /// <summary>Closed around the player's augment: its node was given back, so all that's left is to
        /// take the augment out.</summary>
        Held
    }

    /// <summary>One place on an ability's ring of augment slots.</summary>
    /// <param name="OpenerId">The node that must be taken for the slot to exist — the ability node itself
    /// for the tier that comes with it, a socket node for the rest.</param>
    /// <param name="Tier">Which tier of augment the slot accepts.</param>
    /// <param name="Address">The slot's signature, built with the same constructor the allocation uses,
    /// so a pip and a socket are never two spellings of one.</param>
    public readonly record struct SocketRingSlot(string OpenerId, int Tier, string Address);

    /// <summary>
    /// Which augment slots an ability node stands for — the composition of the ring drawn around it. Read
    /// off the DOCUMENT alone: composition follows authoring and changes only on catalog load, while each
    /// place's current state follows the board and changes every purchase — kept apart, composition is
    /// collected once and state is a lookup at draw time.
    /// <para>Order is the board's own (<see cref="IAbilitySocketBoard.SocketsOf"/>): tier, then address —
    /// never node id. The address carries a separator that sorts after letters, so two ids sharing a
    /// prefix order one way and their addresses the other, and the Nth pip would stop being the Nth
    /// slot.</para>
    /// </summary>
    public static class SocketRings
    {
        /// <summary>Fills <paramref name="rings"/> with one entry per node that hands over an ability
        /// (<see cref="NodeKindRules.UnlocksAbility"/>). A node naming no ability (the neutral core seed)
        /// gets no ring, since it opens nothing.</summary>
        public static void Collect(PassiveTreeDocument document, IDictionary<string, List<SocketRingSlot>> rings)
        {
            rings.Clear();

            foreach (PassiveNode node in document.Nodes)
            {
                if (!NodeKindRules.UnlocksAbility(node.Kind) || string.IsNullOrWhiteSpace(node.AbilityId)) continue;

                rings[node.Id] = SlotsOf(document, node);
            }
        }

        /// <summary>The board's answer for one place of a ring, verbatim — so a wheel pip, a popup line
        /// and anything else asking share one reading. A closed slot is in the vocabulary because the
        /// player's augment is in it.</summary>
        public static SocketSlotState StateOf(AbilitySocket? socket) => socket switch
        {
            null => SocketSlotState.Unopened,
            { IsOpen: false } => SocketSlotState.Held,
            { IsEmpty: true } => SocketSlotState.Open,
            _ => SocketSlotState.Filled
        };

        /// <summary>The ability node's own slot plus every socket node pointing at the same ability. The
        /// tier-1 slot belongs to the ability rather than a node of its own, hence it's carried by the
        /// unlock node here.</summary>
        private static List<SocketRingSlot> SlotsOf(PassiveTreeDocument document, PassiveNode unlock)
        {
            List<SocketRingSlot> slots = [Slot(unlock.Id, unlock.AbilityId, NodeKindRules.UnlockSocketTier)];

            foreach (PassiveNode node in document.Nodes)
            {
                int tier = NodeKindRules.SocketTier(node.Kind);
                if (tier == NodeKindRules.NoSocket || tier == NodeKindRules.UnlockSocketTier) continue;
                if (!string.Equals(node.AbilityId, unlock.AbilityId, StringComparison.Ordinal)) continue;

                slots.Add(Slot(node.Id, unlock.AbilityId, tier));
            }

            return
            [
                .. slots
                    .OrderBy(slot => slot.Tier)
                    .ThenBy(slot => slot.Address, StringComparer.Ordinal)
            ];
        }

        private static SocketRingSlot Slot(string openerId, string abilityId, int tier) =>
            new(openerId, tier, new AbilitySocketPlacement(openerId, abilityId, tier).Address);
    }
}
