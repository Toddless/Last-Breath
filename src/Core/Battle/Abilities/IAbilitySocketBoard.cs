namespace Core.Battle.Abilities
{
    using System.Collections.Generic;

    /// <summary>
    /// Every augment slot the character owns. The set of slots is not the board's to invent: it is
    /// handed the placements the allocation opens and holds exactly those, so a slot exists while
    /// the node behind it is taken and stops existing when the node is given back.
    ///
    /// What the board does own is the occupants — which augment sits in which slot — because that
    /// survives a save and the placements do not (they are re-read from the tree on every load).
    /// </summary>
    public interface IAbilitySocketBoard
    {
        /// <summary>Every open socket, empty ones included.</summary>
        IReadOnlyList<AbilitySocket> Sockets { get; }

        /// <summary>
        /// Every augment the character owns, each with the slot it belongs to: the occupants of the
        /// open sockets, plus the saved entries no allocation has been able to judge yet — held only
        /// until the first <see cref="Sync"/> answers for them and never past it, so nothing outside
        /// the sockets can shadow what is in one. This is what a save file writes — the slots
        /// themselves come back from the tree.
        /// </summary>
        IReadOnlyCollection<AbilitySocketOccupant> Occupants { get; }

        /// <summary>The open sockets of one ability, in ascending tier order.</summary>
        IReadOnlyList<AbilitySocket> SocketsOf(string abilityId);

        AbilitySocket? Find(string socketId);

        /// <summary>Puts an augment into a free socket. False when the socket does not exist or is
        /// already occupied — the caller learns nothing happened instead of losing what was in.</summary>
        bool Install(string socketId, string augmentId);

        /// <summary>Takes the augment out and hands it back; null when the socket is unknown or free.</summary>
        string? Extract(string socketId);

        /// <summary>
        /// Makes the board hold exactly these sockets. A placement that was already open keeps its
        /// occupant; a socket the set does not name is dropped along with whatever was in it, because
        /// the node that justified it is gone. A placement whose ability or tier no longer matches the
        /// socket of that id is a node repointed between builds: the slot is rebuilt and its old
        /// occupant goes with the ability it was chosen for.
        /// Calling this is also how the board learns that an allocation can be read at all — the
        /// caller only has one when it has a tree document to read it from — and it is where the
        /// entries held for want of one are finally answered: each goes into the slot it names while
        /// that slot is open under the signature it was chosen under, and is dropped where it is not.
        /// Afterwards the board holds nothing outside its sockets.
        /// </summary>
        void Sync(IReadOnlyCollection<AbilitySocketPlacement> open);

        /// <summary>
        /// Makes the saved occupants the whole truth: every socket is emptied first, then each entry
        /// goes back into the slot it names — but only while that slot is still the one the augment
        /// was chosen for. An entry naming a socket no taken node opens, or one whose ability or tier
        /// has moved since it was written, is dropped: the slots are the allocation's to give, and an
        /// augment picked for one ability must not end up worn by another.
        /// Entries arriving before any allocation has been read are neither placed nor dropped but
        /// held (see <see cref="Occupants"/>), so that they are written back unchanged: a launch whose
        /// tree failed to load knows nothing about which slots exist, and must not spend the player's
        /// augments on the way to the next save. The postponement ends at the first <see cref="Sync"/>,
        /// which puts them to the allocation it carries.
        /// </summary>
        void Restore(IReadOnlyCollection<AbilitySocketOccupant> saved);
    }
}
