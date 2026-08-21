namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;

    /// <summary>
    /// The one gate an augment goes through on its way into a slot, and the one place that answers
    /// whether it could. Both questions are the same order of checks — does the bag hold it, is there a
    /// slot, is the slot still backed by a node, is it free, does the rule take it — so they are one
    /// object with the mutation switched off in one of them. A preview written beside the install
    /// instead of inside it would be a second reading of that order, free to answer differently from
    /// the gate it is supposed to be previewing.
    /// </summary>
    public interface IAugmentInstallGate
    {
        /// <summary>What an install would answer right now, moving nothing. Synchronous because the one
        /// caller that needs it — the engine's drop check — is: it must answer inside the frame the
        /// pointer moved in, and the request bus cannot. Advisory, never authoritative: the bag can be
        /// emptied and a node refunded between the hover and the drop, and the answer that counts is
        /// the one <see cref="Install"/> gives.</summary>
        AugmentInstallResult Judge(string socketAddress, string itemInstanceId);

        /// <summary>
        /// Which of the carried augments this slot would take, in the order they are offered: the
        /// strongest tier first, and within a tier the best copy first. The whole question the picker
        /// of an empty slot asks, answered here rather than by a window walking the bag: the window
        /// would be measuring tiers and tags a second time, and would end up offering what the gate
        /// below then refuses.
        /// </summary>
        IReadOnlyList<IAugmentItem> Candidates(string socketAddress);

        /// <summary>Moves the copy out of the bag and into the slot. A refusal moves nothing and names
        /// which gate answered.</summary>
        AugmentInstallResult Install(string socketAddress, string itemInstanceId);
    }

    /// <inheritdoc cref="IAugmentInstallGate"/>
    /// <remarks>
    /// Order is the whole of it: the board takes the copy FIRST and the item leaves the bag only once it
    /// has — a refusal then costs nothing, and a seating never leaves the same copy lying in the bag as
    /// well, where it could be seated a second time or sold off the character's own build.
    /// <para>
    /// A refusal is named rather than counted: the fitting rule's verdict is carried back untouched, so
    /// a window tells the player why the augment stayed out instead of measuring tiers and tags on its
    /// own — two readings of one rule are two answers.
    /// </para>
    /// <para>
    /// A seating that took ends by putting the board back onto the abilities. The gate does not work out
    /// which upgrade that is — the binder reads the whole arrangement, the same way it reads it after a
    /// load or a refunded node — because a gate that dressed the ability itself would be a second
    /// opinion about what the sockets hold.
    /// </para>
    /// </remarks>
    /// <param name="inventory">Optional: a composition without a bag (the battle sandbox) holds no
    /// augment to move, and every id offered to it is honestly an id it does not carry.</param>
    /// <param name="records">Optional: what the augment ids mean, for the one thing the verdict does
    /// not say — which tier a copy is written at, and therefore where it stands in the list a slot
    /// offers. A composition without a catalog seats what it is handed (see
    /// <see cref="AbilitySocketBoard"/>), so there is no tier to rank by either and the offer keeps the
    /// rest of its order — rarity, then name.</param>
    public sealed class AugmentInstallGate(
        IAbilitySocketBoard sockets,
        IAbilityAugmentBinder augments,
        IInventory? inventory = null,
        IAbilityAugmentCatalog? records = null)
        : IAugmentInstallGate
    {
        public AugmentInstallResult Judge(string socketAddress, string itemInstanceId) =>
            inventory?.GetItem<IAugmentItem>(itemInstanceId) is not { } item
                ? new AugmentInstallResult(AugmentInstallOutcome.AugmentNotHeld)
                : Verdict(socketAddress, item.Augment);

        /// <summary>
        /// The bag put to the same order of checks an install goes through, copy by copy. The first
        /// gate of that order — does the bag hold it — is answered by where the copies come from, so
        /// what is left is exactly the verdict a drop would get.
        /// <para>
        /// Ordered as the player reads it: tier down, because the tier is what a slot is spent on, then
        /// rarity down, because within one tier the rarity is the copy's numbers. The name breaks a
        /// remaining tie so two copies of one record never swap places between two openings.
        /// </para>
        /// </summary>
        public IReadOnlyList<IAugmentItem> Candidates(string socketAddress) =>
        [
            .. Carried()
                .Where(item => Verdict(socketAddress, item.Augment).Installed)
                .OrderByDescending(TierOf)
                .ThenBy(item => item.Rarity.DisplayRank())
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .ThenBy(item => item.InstanceId, StringComparer.Ordinal)
        ];

        public AugmentInstallResult Install(string socketAddress, string itemInstanceId)
        {
            if (inventory?.GetItem<IAugmentItem>(itemInstanceId) is not { } item)
                return new AugmentInstallResult(AugmentInstallOutcome.AugmentNotHeld);

            AugmentInstallResult verdict = Verdict(socketAddress, item.Augment);
            if (!verdict.Installed) return verdict;

            if (!sockets.Install(socketAddress, item.Augment)) return Verdict(socketAddress, item.Augment);

            inventory.RemoveItemByInstanceId(item.InstanceId);
            augments.Bind();
            return verdict;
        }

        /// <summary>The gates in the order the player should hear about them. A slot the allocation no
        /// longer backs is answered BEFORE the question of what is in it: "that slot is gone" is the
        /// truth, and "the slot is busy" would send him looking for a way to empty it.</summary>
        private AugmentInstallResult Verdict(string socketAddress, AugmentInstance augment)
        {
            if (sockets.Find(socketAddress) is not { } socket)
                return new AugmentInstallResult(AugmentInstallOutcome.NoSuchSocket);

            if (!socket.IsOpen) return new AugmentInstallResult(AugmentInstallOutcome.SocketClosed);

            if (!socket.IsEmpty) return new AugmentInstallResult(AugmentInstallOutcome.SocketOccupied);

            AugmentFitResult? fit = sockets.Judge(socketAddress, augment);
            return fit == AugmentFitResult.Fits
                ? new AugmentInstallResult(AugmentInstallOutcome.Installed)
                : new AugmentInstallResult(AugmentInstallOutcome.DoesNotFit, fit);
        }

        /// <summary>The augments in the bag. A composition without one carries none.</summary>
        private IEnumerable<IAugmentItem> Carried() =>
            (inventory?.GetContents() ?? []).Select(entry => entry.Item).OfType<IAugmentItem>();

        /// <summary>The tier the copy's record is written at — the record's and never the copy's own,
        /// for the same reason the fitting rule reads the record: a lucky roll does not move an augment
        /// up a tier.</summary>
        private int TierOf(IAugmentItem item) => records?.Find(item.Id)?.Tier ?? 0;
    }
}
