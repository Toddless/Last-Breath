namespace Battle.Source.Abilities
{
    using System;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Inventory;
    using Core.Items;
    using Core.Services;

    /// <summary>
    /// The one gate an ornament goes through between the bag and an ability, both ways. One object for
    /// the two directions because they are one rule read from either end: an ability wears at most one
    /// ornament, and an ornament comes off only once its socket is empty.
    /// </summary>
    public interface IOrnamentAttachGate
    {
        /// <summary>Moves the ornament out of the bag and onto the ability, opening the socket it grants.
        /// A refusal moves nothing and names which gate answered.</summary>
        OrnamentAttachResult Attach(string itemInstanceId, string abilityId);

        /// <summary>Takes the ornament off and back into the bag, and its socket with it. A refusal
        /// leaves it exactly where it was.</summary>
        OrnamentDetachResult Detach(string ornamentId);
    }

    /// <inheritdoc cref="IOrnamentAttachGate"/>
    /// <remarks>
    /// Neither direction binds anything onto the abilities, and that is a consequence of the remove-only
    /// rule rather than an omission: an attach opens an EMPTY socket and a detach is refused while its
    /// socket holds anything, so the set of augments the abilities wear is the same before and after.
    /// The augments' own two roads (<see cref="IAugmentInstallGate"/> and the extract handler) are still
    /// the only places that arrangement changes.
    /// <para>
    /// The orders mirror the augment gate's. Attaching takes the ornament onto the ability FIRST and out
    /// of the bag after, so a refusal costs nothing and a fitted ornament never lies in the bag as well.
    /// Detaching puts the item in the bag FIRST — a full bag is a refusal rather than an ornament
    /// dropped on the way home.
    /// </para>
    /// </remarks>
    /// <param name="inventory">Optional: a composition without a bag (the battle sandbox) holds no
    /// ornament to move, and every id offered to it is honestly an id it does not carry.</param>
    public sealed class OrnamentAttachGate(
        IAbilitySocketBoard sockets,
        IPlayerAccessor players,
        IOrnamentMinter minter,
        IInventory? inventory = null)
        : IOrnamentAttachGate
    {
        public OrnamentAttachResult Attach(string itemInstanceId, string abilityId)
        {
            if (inventory?.GetItem<IOrnamentItem>(itemInstanceId) is not { } item)
                return OrnamentAttachResult.OrnamentNotHeld;

            if (Verdict(item, abilityId) is var verdict && verdict != OrnamentAttachResult.Attached) return verdict;

            // The invariant lives on the board, so its answer is the one that counts. A refusal here is
            // asked again rather than labelled from memory — and if the order above cannot account for it,
            // it is reported as the board's own refusal instead of borrowing a reason that would be false.
            if (!sockets.Attach(new AbilityOrnament(item.Id, abilityId, item.Tier)))
                return Verdict(item, abilityId) is var second && second != OrnamentAttachResult.Attached
                    ? second
                    : OrnamentAttachResult.BoardRefused;

            inventory.RemoveItemByInstanceId(item.InstanceId);
            return OrnamentAttachResult.Attached;
        }

        /// <summary>What an attach would answer right now, moving nothing — the gates in the order the
        /// player should hear about them. <see cref="OrnamentAttachResult.Attached"/> means only that
        /// nothing here refuses it.</summary>
        private OrnamentAttachResult Verdict(IOrnamentItem item, string abilityId)
        {
            // The design's single gate, and it is deliberately only this one: an open socket of the same
            // tier is NOT required, so the tier-3 ornament may go onto an ability the tree never reached.
            if (!IsUnlocked(abilityId)) return OrnamentAttachResult.AbilityNotUnlocked;

            if (sockets.Attachment(item.Id) != null) return OrnamentAttachResult.AlreadyAttached;

            return sockets.OrnamentOn(abilityId) != null
                ? OrnamentAttachResult.AbilityAlreadyOrnamented
                : OrnamentAttachResult.Attached;
        }

        public OrnamentDetachResult Detach(string ornamentId)
        {
            if (sockets.Attachment(ornamentId) is not { } worn) return OrnamentDetachResult.NotAttached;

            if (sockets.Find(worn.Slot.Address) is { IsEmpty: false }) return OrnamentDetachResult.SocketNotEmpty;

            if (minter.Mint(ornamentId) is not { } item) return OrnamentDetachResult.CannotBeHeld;

            if (inventory?.TryAddItem(item) != true) return OrnamentDetachResult.NoBagRoom;

            if (sockets.Detach(ornamentId)) return OrnamentDetachResult.Detached;

            // Unreachable through the checks above, and undone rather than trusted: the bag already holds
            // the ornament, and leaving it there beside the ability wearing it is the one outcome that
            // duplicates a thing there are three of in the game.
            inventory.RemoveItemByInstanceId(item.InstanceId);
            return OrnamentDetachResult.SocketNotEmpty;
        }

        /// <summary>Whether the player holds that ability at all. The book is the live truth of what he
        /// owns — the allocation is what fills it, and asking the tree instead would be a second reading
        /// of the same question.</summary>
        private bool IsUnlocked(string abilityId) =>
            players.Player?.AbilityBook.AllAbilities.Any(ability =>
                string.Equals(ability.Id, abilityId, StringComparison.Ordinal)) == true;
    }
}
