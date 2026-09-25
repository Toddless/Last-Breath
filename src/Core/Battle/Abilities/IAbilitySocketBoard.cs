namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Every augment slot the character owns. Which slots are LIVE comes from the allocation via
    /// <see cref="Sync"/>; what the board owns is the occupants and the ornaments, because those survive a
    /// save and the placements do not (they are re-read from the tree on every load).
    /// A slot holding an augment is never destroyed with its node — it goes CLOSED
    /// (<see cref="AbilitySocket.IsOpen"/>) and stays as a way to take the augment out; an empty one goes.
    /// Slots are addressed by their whole signature (<see cref="AbilitySocketPlacement.Address"/>), not by
    /// the node that opened them, so a repointed node leaves the old closed slot beside the new live one.
    /// </summary>
    public interface IAbilitySocketBoard
    {
        /// <summary>The set of slots or their contents moved. Raised only on an actual change, so a
        /// redraw hooked to it cannot loop through the reconcile passes underneath.</summary>
        event Action? Changed;

        /// <summary>Every socket, empty and closed ones included.</summary>
        IReadOnlyList<AbilitySocket> Sockets { get; }

        /// <summary>Every augment the character owns with the slot it belongs to, closed slots included —
        /// a refunded node does not take the player's property with it. This is what a save writes.</summary>
        IReadOnlyCollection<AbilitySocketOccupant> Occupants { get; }

        /// <summary>The ornaments the character's abilities wear — saved for the same reason as the
        /// occupants: where each one sits is a player decision no allocation reproduces.</summary>
        IReadOnlyCollection<AbilityOrnament> Ornaments { get; }

        /// <summary>The sockets of one ability, in ascending tier order and stable within a tier. Closed
        /// ones are in it — they still hold something to take out.</summary>
        IReadOnlyList<AbilitySocket> SocketsOf(string abilityId);

        AbilitySocket? Find(string socketAddress);

        /// <summary>Puts an augment into a free socket. False, and nothing changed at all, when the socket
        /// is unknown, closed, occupied or does not take that augment (<see cref="AugmentFit"/>) — a refused
        /// augment stays with the caller rather than disappearing into a slot where it would sit inert.</summary>
        bool Install(string socketAddress, AugmentInstance augment);

        /// <summary>How the slot judges that augment without seating it, from the one place the rule is
        /// written (<see cref="AugmentFit"/>), so a refusal can be explained to the player. Null when there
        /// is nothing to judge: no such slot, or no record declares the augment. Whether the slot is closed
        /// is not this rule's question — the gate above answers it first.</summary>
        AugmentFitResult? Judge(string socketAddress, AugmentInstance augment);

        /// <summary>Takes the augment out and hands back the copy that went in, with the numbers it was
        /// minted with. Null when the socket is unknown or free. A closed socket emptied this way ceases to
        /// exist — it was only ever the way back out.</summary>
        AugmentInstance? Extract(string socketAddress);

        /// <summary>
        /// Makes exactly these placements the board's LIVE slots. A placement already standing keeps its
        /// occupant and a closed one comes back to life around it (same signature, same slot); one the set
        /// does not name is dropped when empty and closed when not.
        /// The ornaments' slots are the board's own: they are not in the set and are not retired by their
        /// absence from it, so a respec cannot spend them.
        /// </summary>
        void Sync(IReadOnlyCollection<AbilitySocketPlacement> open);

        /// <summary>The ornament that ability wears, or null while it wears none. At most one — the rule
        /// that keeps the fourth socket a fourth socket.</summary>
        AbilityOrnament? OrnamentOn(string abilityId);

        /// <summary>Where that ornament currently is, or null while nothing wears it — the other side of
        /// <see cref="OrnamentOn"/>, since there is only one of each ornament.</summary>
        AbilityOrnament? Attachment(string ornamentId);

        /// <summary>
        /// Puts an ornament on an ability and opens the slot it grants — one more of exactly its own tier.
        /// False, moving nothing, when the ability already wears an ornament or this ornament is on another
        /// one; the invariant stands here so every road obeys it, a load included. Moving an ornament is
        /// <see cref="Detach"/> then this, never a swap: a swap would carry an augment onto an ability whose
        /// tags the fitting rule never measured it against.
        /// </summary>
        bool Attach(AbilityOrnament ornament);

        /// <summary>Takes the ornament off and its granted slot with it. False while that slot holds an
        /// augment (remove-only, like a slot whose node was given back) and false for an unworn
        /// ornament.</summary>
        bool Detach(string ornamentId);

        /// <summary>
        /// Makes the saved occupants the whole truth: live sockets are emptied and closed ones dropped,
        /// then every entry goes back into the slot it names. Where the allocation opens that slot the
        /// augment lands live; where it does not, the entry brings its own slot back CLOSED and is written
        /// into the next save unchanged. Nothing is ever dropped — a launch that could read no allocation
        /// lands everything closed, and the next <see cref="Sync"/> wakes up what it names.
        /// The fitting rule is not put again: it answered when the player seated the augment, so a record
        /// edited between builds costs him the USE of what he owns rather than the thing, and an
        /// arrangement today's records would refuse is reported instead.
        /// </summary>
        /// <param name="ornaments">Where the ornaments were. In the SAME call as the occupants, because
        /// half a restored arrangement is a character whose fourth socket quietly went missing. Put on
        /// before the occupants land, though nothing rests on that order.</param>
        /// <returns>The ornaments that could NOT go back on — a second one named for an ability that
        /// already has one. Each comes from an unrepeatable quest and is now nowhere, so it is handed to the
        /// caller to place rather than reported and forgotten. An ornament the file merely names TWICE is
        /// not in here: it is already on an ability.</returns>
        IReadOnlyCollection<AbilityOrnament> Restore(
            IReadOnlyCollection<AbilitySocketOccupant> saved,
            IReadOnlyCollection<AbilityOrnament> ornaments);
    }
}
