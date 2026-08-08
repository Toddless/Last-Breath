namespace Core.PassiveTree.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;

    /// <summary>What one place of an ability's ring of slots is doing right now.</summary>
    public enum SocketSlotState
    {
        /// <summary>The node that would open the slot has not been bought. The board holds a socket
        /// exactly while its node is taken, so "no socket at that address" IS "the node is not owned" —
        /// and a node the player has only MARKED never reaches the board, which is what keeps a plan out
        /// of this answer without a rule of its own.</summary>
        Unopened,

        /// <summary>Lit and free.</summary>
        Open,

        /// <summary>Lit and wearing an augment.</summary>
        Filled,

        /// <summary>Closed around the player's augment: the node behind it was given back, so the only
        /// thing left to do with it is take the augment out.</summary>
        Held
    }

    /// <summary>One place on an ability's ring of augment slots.</summary>
    /// <param name="OpenerId">The node that has to be taken for the slot to exist — the ability node
    /// itself for the tier that comes with the ability, a socket node for the rest.</param>
    /// <param name="Tier">Which tier of augment the slot accepts.</param>
    /// <param name="Address">The slot's whole signature, built by the same constructor the allocation
    /// builds it with, so a pip and a socket are the same string and never two spellings of one.</param>
    public readonly record struct SocketRingSlot(string OpenerId, int Tier, string Address);

    /// <summary>
    /// Which augment slots an ability node stands for — the composition of the ring drawn around it.
    ///
    /// <para>Read off the DOCUMENT alone and nothing else. What a ring is made of follows from how the
    /// tree was authored and changes once a catalog load; what each place of it is doing right now
    /// follows from the board and changes on every purchase. Keeping the two readings apart is what lets
    /// the composition be collected once and the state be a lookup at drawing time.</para>
    ///
    /// <para>The order is the board's own (<see cref="IAbilitySocketBoard.SocketsOf"/>): tier, then
    /// address. Never the node id — the address carries a separator that sorts after letters, so two ids
    /// sharing a prefix order one way and their addresses the other, and the Nth pip would stop being the
    /// Nth slot.</para>
    /// </summary>
    public static class SocketRings
    {
        /// <summary>
        /// Fills <paramref name="rings"/> with one entry per node that hands an ability over
        /// (<see cref="NodeKindRules.UnlocksAbility"/>) and names which. A node naming no ability — the
        /// neutral seed at the core — gets no ring at all, because it opens nothing.
        /// </summary>
        public static void Collect(PassiveTreeDocument document, IDictionary<string, List<SocketRingSlot>> rings)
        {
            rings.Clear();

            foreach (PassiveNode node in document.Nodes)
            {
                if (!NodeKindRules.UnlocksAbility(node.Kind) || string.IsNullOrWhiteSpace(node.AbilityId)) continue;

                rings[node.Id] = SlotsOf(document, node);
            }
        }

        /// <summary>What the board says about one place of a ring. The board's answer and nothing derived
        /// beside it, so a pip on the wheel, a line in a popup and anything else that asks are one
        /// reading. A closed slot is in the vocabulary because the player's augment is in it.</summary>
        public static SocketSlotState StateOf(AbilitySocket? socket) => socket switch
        {
            null => SocketSlotState.Unopened,
            { IsOpen: false } => SocketSlotState.Held,
            { IsEmpty: true } => SocketSlotState.Open,
            _ => SocketSlotState.Filled
        };

        /// <summary>The ability node's own slot plus every socket node of the document pointing at the
        /// same ability. The tier-1 slot belongs to the ability rather than to a node of its own, which is
        /// why the unlock node carries it here exactly as the allocation hands it over.</summary>
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
