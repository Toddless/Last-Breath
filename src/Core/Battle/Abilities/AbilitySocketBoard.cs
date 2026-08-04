namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.AbilityData;
    using Session;

    /// <inheritdoc cref="IAbilitySocketBoard"/>
    /// <param name="augments">Optional: what the augment ids mean. The catalog is the records the
    /// fitting rule reads, so a composition that supplies none leaves the board a plain slot-holder:
    /// it seats what it is handed, as it did before there was a rule. That is the contract, not a
    /// hole waiting for a guard — with no records there is nothing for the rule to be right about,
    /// and refusing every id instead would take the sockets of that composition down with it. Where
    /// a catalog is supplied, every seating goes through <see cref="AugmentFit"/>.</param>
    public sealed class AbilitySocketBoard(IAbilityAugmentCatalog? augments = null)
        : IAbilitySocketBoard, ISessionResettable
    {
        private readonly Dictionary<string, AbilitySocket> _sockets = new(StringComparer.Ordinal);

        /// <summary>Saved augments no slot could take, held because nothing has been able to judge
        /// them yet. Filled only on a board no allocation has ever spoken to, and emptied by the first
        /// one that does: an entry the allocation opens its slot for is seated, the rest is dropped.</summary>
        private readonly List<AbilitySocketOccupant> _waiting = [];

        /// <summary>Whether <see cref="Sync"/> has ever run. Until it has, "no slot of that id" means
        /// the slots are unknown, not that the allocation refused one.</summary>
        private bool _allocationRead;

        public IReadOnlyList<AbilitySocket> Sockets => [.. _sockets.Values];

        public IReadOnlyCollection<AbilitySocketOccupant> Occupants => [.. Installed(), .. _waiting];

        public IReadOnlyList<AbilitySocket> SocketsOf(string abilityId) =>
        [
            .. _sockets.Values
                .Where(socket => string.Equals(socket.AbilityId, abilityId, StringComparison.Ordinal))
                .OrderBy(socket => socket.Tier)
        ];

        public AbilitySocket? Find(string socketId) => _sockets.GetValueOrDefault(socketId);

        public bool Install(string socketId, AugmentInstance augment) =>
            Find(socketId) is { } socket && Accepts(socket, augment) && socket.Install(augment);

        public AugmentInstance? Extract(string socketId) => Find(socketId)?.Extract();

        public void Sync(IReadOnlyCollection<AbilitySocketPlacement> open)
        {
            HashSet<string> opened = new(StringComparer.Ordinal);

            foreach (AbilitySocketPlacement placement in open)
            {
                opened.Add(placement.SocketId);
                if (Matches(Find(placement.SocketId), placement)) continue;

                _sockets[placement.SocketId] = new AbilitySocket(placement.SocketId, placement.AbilityId, placement.Tier);
            }

            foreach (string closed in _sockets.Keys.Where(socketId => !opened.Contains(socketId)).ToList())
                _sockets.Remove(closed);

            _allocationRead = true;
            JudgeHeld();
        }

        public void Restore(IReadOnlyCollection<AbilitySocketOccupant> saved)
        {
            foreach (AbilitySocket socket in _sockets.Values) socket.Extract();
            _waiting.Clear();

            foreach (AbilitySocketOccupant occupant in saved)
                if (!Seat(occupant) && !_allocationRead) _waiting.Add(occupant);
        }

        /// <summary>Back to the board of a fresh character: no slots, and nothing waiting outside
        /// them. The sockets themselves come back from the allocation, so a tree that can be read
        /// empties the board through its own reset — but a launch whose document never parsed syncs
        /// nothing at all, and the entries held from the file it loaded would otherwise be written
        /// into the save of the playthrough that follows.</summary>
        public void ResetSession()
        {
            _sockets.Clear();
            _waiting.Clear();
        }

        /// <summary>Puts the held entries to the allocation that has just spoken: one naming a slot
        /// it opens under the same signature goes in, the rest are dropped. The holding lasts until
        /// the first allocation able to answer for them and not a pass longer — an entry kept past
        /// that point is an augment outside every slot, and it would go on being written under a
        /// socket id whose slot the player has since filled himself.</summary>
        private void JudgeHeld()
        {
            foreach (AbilitySocketOccupant occupant in _waiting) Seat(occupant);

            _waiting.Clear();
        }

        /// <summary>Whether the slot takes that augment. The rule is not written here — the board only
        /// turns what it holds into what <see cref="AugmentFit"/> judges: the augment's record and the
        /// tags of the slot's ability. The RECORD is what is measured and never the copy's own numbers:
        /// where an augment belongs is a property of the augment, and a lucky roll does not open a slot
        /// an unlucky one is refused. An id the catalog does not hold is refused; there is no record to
        /// measure, and seating one on the strength of its spelling is how an augment ends up in a slot
        /// nothing ever agreed to.</summary>
        private bool Accepts(AbilitySocket socket, AugmentInstance occupant)
        {
            if (augments is null) return true;

            AbilityUpgradeData? augment = augments.Find(occupant.AugmentId);
            return augment is not null
                   && AugmentFit.Check(
                       socket.Placement,
                       augments.TagsOf(socket.AbilityId),
                       augment,
                       WornGroups(socket.AbilityId)) == AugmentFitResult.Fits;
        }

        /// <summary>The exclusion groups already standing in one ability's slots. Only that ability's
        /// own sockets are counted: the group says one of these at a time on one ability, not one of
        /// these in the whole build.</summary>
        private IReadOnlyCollection<string> WornGroups(string abilityId)
        {
            HashSet<string> worn = new(StringComparer.Ordinal);

            foreach (AbilitySocket socket in SocketsOf(abilityId))
            {
                if (socket.Augment is not { } installed) continue;

                string? group = augments?.Find(installed.AugmentId)?.ExclusionGroup;
                if (!string.IsNullOrWhiteSpace(group)) worn.Add(group);
            }

            return worn;
        }

        /// <summary>The augments sitting in the open slots, each paired with the slot it sits in.</summary>
        private IEnumerable<AbilitySocketOccupant> Installed()
        {
            foreach (AbilitySocket socket in _sockets.Values)
                if (socket.Augment is { } augment)
                    yield return new AbilitySocketOccupant(socket.Placement, augment);
        }

        /// <summary>Puts one saved augment back where the file says it was. Refused when no slot of
        /// that id is open, or when the slot standing there is not the one the augment was chosen for
        /// — the id is all a file can name, and a build is free to have moved the node behind it — or
        /// when the augment no longer fits that slot. A file records what was seated, not permission
        /// to seat it again: the records themselves move between builds, and a load puts the same
        /// question to the same rule as a player's own install.</summary>
        private bool Seat(AbilitySocketOccupant occupant) =>
            Matches(Find(occupant.Slot.SocketId), occupant.Slot) && Install(occupant.Slot.SocketId, occupant.Augment);

        /// <summary>Whether the slot standing at that id is still the slot described. A mismatch means
        /// the node was repointed, and the occupant belongs to what the node used to be — the socket is
        /// rebuilt rather than quietly re-labelled around its contents. This is the signature of a
        /// placement, not the fitting rule: it asks whether the node moved, and says nothing about
        /// which augments the slot takes (<see cref="AugmentFit"/>).</summary>
        private static bool Matches(AbilitySocket? socket, AbilitySocketPlacement placement) =>
            socket?.Placement == placement;
    }
}
