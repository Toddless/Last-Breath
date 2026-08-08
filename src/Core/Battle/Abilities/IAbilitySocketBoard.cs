namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Every augment slot the character owns. Which slots are LIVE is not the board's to invent: it is
    /// handed the placements the allocation opens and holds exactly those, so a slot works while the
    /// node behind it is taken and stops working when the node is given back.
    ///
    /// What the board does own is the occupants — which augment sits in which slot — because that
    /// survives a save and the placements do not (they are re-read from the tree on every load). Owning
    /// them is why a slot is not simply destroyed with its node: an augment is the player's property,
    /// so a slot with something in it is CLOSED instead (<see cref="AbilitySocket.IsOpen"/>) and lives
    /// on as a place he can take his augment out of and put nothing into. An empty slot has nothing to
    /// protect and goes with the node.
    ///
    /// A slot is addressed by its whole signature (<see cref="AbilitySocketPlacement.Address"/>) rather
    /// than by the node that opened it: a node repointed between builds leaves the old, closed slot
    /// holding an augment chosen under the old signature while the new, live one stands beside it, and
    /// both are the player's.
    /// </summary>
    public interface IAbilitySocketBoard
    {
        /// <summary>The set of slots or their contents moved. Raised only on an actual change — a pass
        /// that finds everything already as it should be is silent, which is what lets a window redraw
        /// itself on this without the defensive reconcile passes underneath turning into a loop.</summary>
        event Action? Changed;

        /// <summary>Every socket, empty and closed ones included.</summary>
        IReadOnlyList<AbilitySocket> Sockets { get; }

        /// <summary>
        /// Every augment the character owns, each with the slot it belongs to — the occupants of the
        /// live sockets and of the closed ones alike, because a refunded node does not take the player's
        /// property with it. This is what a save file writes; the slots themselves come back from the
        /// tree, and which of them are live follows from that.
        /// </summary>
        IReadOnlyCollection<AbilitySocketOccupant> Occupants { get; }

        /// <summary>The sockets of one ability, in ascending tier order and stable within a tier. Closed
        /// ones are in it: they still hold something, and a window that skipped them would show the
        /// player nothing to take it out of.</summary>
        IReadOnlyList<AbilitySocket> SocketsOf(string abilityId);

        AbilitySocket? Find(string socketAddress);

        /// <summary>Puts an augment into a free socket. False when the socket does not exist, is
        /// closed, is already occupied, or does not take that augment (<see cref="AugmentFit"/>) — the
        /// caller learns nothing happened instead of losing what was in. A refusal changes nothing at
        /// all: an augment that does not belong in a slot must come back to whoever offered it, not
        /// disappear into one where it would sit inert.</summary>
        bool Install(string socketAddress, AugmentInstance augment);

        /// <summary>How the slot judges that augment, without seating it. The verdict comes from the
        /// one place the rule is written (<see cref="AugmentFit"/>), so whoever refuses an install can
        /// name the reason to the player instead of measuring tiers and tags a second time.
        /// Null when there is nothing to judge: no slot of that address exists, or no record declares
        /// the augment — the board refuses such an id outright, and a rule with no record in front of
        /// it has nothing to be right about. Whether the slot is CLOSED is not this rule's question:
        /// it is a fact about the slot, and the gate above answers it first.</summary>
        AugmentFitResult? Judge(string socketAddress, AugmentInstance augment);

        /// <summary>Takes the augment out and hands it back — the copy that went in, with the numbers
        /// it was minted with. Null when the socket is unknown or free. A closed socket emptied this way
        /// ceases to exist: it was only ever the way back out.</summary>
        AugmentInstance? Extract(string socketAddress);

        /// <summary>
        /// Makes the board hold exactly these slots as its LIVE ones. A placement already standing keeps
        /// its occupant, and one standing closed comes back to life around it — the same signature is
        /// the same slot, so a node bought back is the place the augment was sitting in. A slot the set
        /// does not name is dropped when empty and closed when not: the node that justified it is gone,
        /// the augment in it is not.
        /// A node repointed at another ability needs no case of its own — its old signature is simply
        /// not in the set and its new one is not on the board, so the old slot closes around what it
        /// holds and the new one opens empty beside it.
        /// </summary>
        void Sync(IReadOnlyCollection<AbilitySocketPlacement> open);

        /// <summary>
        /// Makes the saved occupants the whole truth: the live sockets are emptied and the closed ones
        /// dropped first, then every entry goes back into the slot it names. Where the allocation opens
        /// that slot the augment goes in live; where it does not, the entry brings its own slot back
        /// CLOSED — remove-only, and written into the next save unchanged. Nothing is dropped, because
        /// there is no state of the tree in which losing the player's augments is the right answer: a
        /// launch whose document failed to parse opens no slot at all and lands every entry closed, and
        /// the next <see cref="Sync"/> that can read an allocation wakes up the ones it names.
        /// The fitting rule is not put again here. It answered when the player seated the augment, and
        /// a record edited between builds costs him the USE of what he owns rather than the thing
        /// itself; an arrangement today's records would refuse is reported instead.
        /// </summary>
        void Restore(IReadOnlyCollection<AbilitySocketOccupant> saved);
    }
}
