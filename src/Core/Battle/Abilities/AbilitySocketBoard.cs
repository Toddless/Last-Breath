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
        /// <summary>Every slot the character has, live and closed alike, keyed by
        /// <see cref="AbilitySocketPlacement.Address"/>. The key is the whole signature and not the node
        /// id, which is what lets the slot a refunded node left behind sit beside the live slot the same
        /// node opens after being repointed: both are the player's, and a dictionary keyed by node could
        /// only keep one of them.</summary>
        private readonly Dictionary<string, AbilitySocket> _sockets = new(StringComparer.Ordinal);

        public event Action? Changed;

        public IReadOnlyList<AbilitySocket> Sockets => [.. _sockets.Values];

        public IReadOnlyCollection<AbilitySocketOccupant> Occupants => [.. Installed()];

        public IReadOnlyList<AbilitySocket> SocketsOf(string abilityId) =>
        [
            .. _sockets.Values
                .Where(socket => string.Equals(socket.AbilityId, abilityId, StringComparison.Ordinal))
                .OrderBy(socket => socket.Tier)
                .ThenBy(socket => socket.Address, StringComparer.Ordinal)
        ];

        public AbilitySocket? Find(string socketAddress) => _sockets.GetValueOrDefault(socketAddress);

        public bool Install(string socketAddress, AugmentInstance augment)
        {
            if (Find(socketAddress) is not { } socket
                || Verdict(socket, augment) != AugmentFitResult.Fits
                || !socket.Install(augment)) return false;

            Changed?.Invoke();
            return true;
        }

        public AugmentFitResult? Judge(string socketAddress, AugmentInstance augment) =>
            Find(socketAddress) is { } socket ? Verdict(socket, augment) : null;

        public AugmentInstance? Extract(string socketAddress)
        {
            if (Find(socketAddress) is not { } socket || socket.Extract() is not { } augment) return null;

            // A closed slot exists only to give its augment back; emptied, there is nothing left of it
            // to show and nothing a node backs. This is what takes the last row out of a socket window.
            if (!socket.IsOpen) _sockets.Remove(socketAddress);

            Changed?.Invoke();
            return augment;
        }

        public void Sync(IReadOnlyCollection<AbilitySocketPlacement> open)
        {
            bool moved = false;
            HashSet<string> opened = new(StringComparer.Ordinal);

            foreach (AbilitySocketPlacement placement in open)
            {
                string address = placement.Address;
                opened.Add(address);
                if (_sockets.TryGetValue(address, out AbilitySocket? standing))
                {
                    // The same signature is the same slot: a node bought back is the place the augment
                    // was sitting in, so the slot wakes up around whatever is still in it.
                    if (standing.IsOpen) continue;
                    standing.Reopen();
                    moved = true;
                    continue;
                }

                _sockets[address] = new AbilitySocket(placement.SocketId, placement.AbilityId, placement.Tier);
                moved = true;
            }

            foreach (string address in _sockets.Keys.Where(address => !opened.Contains(address)).ToList())
                moved |= Retire(address);

            if (moved) Changed?.Invoke();
        }

        public void Restore(IReadOnlyCollection<AbilitySocketOccupant> saved)
        {
            // The closed slots of the playthrough being left behind go with it: the board is a
            // singleton, and a slot held open by another character's augment would be written into
            // this one's first save.
            foreach (string address in _sockets.Where(entry => !entry.Value.IsOpen).Select(entry => entry.Key).ToList())
                _sockets.Remove(address);
            foreach (AbilitySocket socket in _sockets.Values) socket.Extract();

            foreach (AbilitySocketOccupant occupant in saved) Seat(occupant);

            // Judged only after every occupant is back: a grantee's fit depends on the donors beside
            // it, and reporting mid-restore would let the order of entries in the file pick the verdict.
            foreach (AbilitySocketOccupant occupant in saved) ReportIllFitting(occupant);

            Changed?.Invoke();
        }

        /// <summary>Back to the board of a fresh character: no slots at all. The sockets themselves come
        /// back from the allocation, so a tree that can be read fills the board again through its own
        /// reset — and a launch whose document never parsed would otherwise carry the closed slots of
        /// the file it loaded into the playthrough that follows.</summary>
        public void ResetSession()
        {
            if (_sockets.Count == 0) return;

            _sockets.Clear();
            Changed?.Invoke();
        }

        /// <summary>What becomes of a slot the allocation no longer opens: an empty one is dropped, one
        /// holding an augment is closed. True when anything changed.</summary>
        private bool Retire(string address)
        {
            AbilitySocket socket = _sockets[address];
            if (socket.IsEmpty)
            {
                _sockets.Remove(address);
                return true;
            }

            if (!socket.IsOpen) return false;

            socket.Close();
            return true;
        }

        /// <summary>
        /// Puts one saved augment back where the file says it was. The live slot of that address takes
        /// it; where the allocation opens no such slot the entry brings its own back CLOSED, so it can
        /// be taken out and nothing else. Nothing is dropped and nothing is judged: the fitting rule
        /// answered once, when the player seated the augment, and a record edited between builds must
        /// cost him the use of what he owns rather than the thing itself. A seating the rule would
        /// refuse today is reported instead, with the address and the verdict, so the edit is visible
        /// to whoever made it.
        /// </summary>
        private void Seat(AbilitySocketOccupant occupant)
        {
            string address = occupant.Slot.Address;

            if (_sockets.TryGetValue(address, out AbilitySocket? standing) && standing.Install(occupant.Augment)) return;

            var closed = new AbilitySocket(occupant.Slot.SocketId, occupant.Slot.AbilityId, occupant.Slot.Tier);
            closed.Install(occupant.Augment); // before Close(): a closed slot refuses every install, this one included
            closed.Close();
            _sockets[address] = closed;
        }

        /// <summary>Says out loud that the file carries an arrangement today's records would not allow.
        /// The augment is seated anyway — see <see cref="Seat"/> — and the line is for whoever moved the
        /// record, who would otherwise find out from a player. The occupant's own socket is left out of
        /// both counts: seated first, it would report itself for wearing its own group.</summary>
        private void ReportIllFitting(AbilitySocketOccupant occupant)
        {
            if (augments is null) return;

            string address = occupant.Slot.Address;
            AbilityAugmentData? record = augments.Find(occupant.Augment.AugmentId);
            IReadOnlyCollection<string> granted = GrantedTags(occupant.Slot.AbilityId, excludingAddress: address);
            AugmentFitResult? verdict = record is null
                ? null
                : AugmentFit.Check(
                    occupant.Slot,
                    augments.TagsOf(occupant.Slot.AbilityId),
                    record,
                    WornGroups(occupant.Slot.AbilityId, excludingAddress: address),
                    granted);

            if (verdict == AugmentFitResult.Fits) return;

            // The one sanctioned NoSharedTag: a grantee outliving its donor beside donors still on the
            // ability. With no donor left there at all no survivor is possible — the verdict is a data
            // edit and is said out loud like any other.
            if (verdict == AugmentFitResult.NoSharedTag && granted.Count > 0) return;

            Tracker.TrackError(
                $"Saved augment '{occupant.Augment.AugmentId}' no longer fits the slot it was seated in " +
                $"({address}): {verdict?.ToString() ?? "no record declares it"}. It is restored so the player keeps it.",
                this);
        }

        /// <summary>How the slot judges that augment. The rule is not written here — the board only
        /// turns what it holds into what <see cref="AugmentFit"/> measures: the augment's record and the
        /// tags of the slot's ability. The RECORD is what is measured and never the copy's own numbers:
        /// where an augment belongs is a property of the augment, and a lucky roll does not open a slot
        /// an unlucky one is refused. An id the catalog does not hold is refused with no verdict at all;
        /// there is no record to measure, and seating one on the strength of its spelling is how an
        /// augment ends up in a slot nothing ever agreed to. A composition supplying no catalog takes
        /// what it is handed, which is the contract this board is built on.</summary>
        private AugmentFitResult? Verdict(AbilitySocket socket, AugmentInstance occupant)
        {
            if (augments is null) return AugmentFitResult.Fits;

            AbilityAugmentData? augment = augments.Find(occupant.AugmentId);
            return augment is null
                ? null
                : AugmentFit.Check(
                    socket.Placement,
                    augments.TagsOf(socket.AbilityId),
                    augment,
                    WornGroups(socket.AbilityId),
                    GrantedTags(socket.AbilityId));
        }

        /// <summary>The tags the ability's installed augments grant it — read off what the ability
        /// WEARS, like <see cref="WornGroups"/>: an augment in a closed slot does nothing and grants
        /// nothing.</summary>
        private IReadOnlyCollection<string> GrantedTags(string abilityId, string? excludingAddress = null)
        {
            HashSet<string> granted = new(StringComparer.OrdinalIgnoreCase);

            foreach (AbilitySocket socket in SocketsOf(abilityId))
            {
                if (string.Equals(socket.Address, excludingAddress, StringComparison.Ordinal)) continue;
                if (socket.WorkingAugment is not { } installed) continue;

                foreach (string tag in augments?.Find(installed.AugmentId)?.GrantsTags ?? [])
                    granted.Add(tag);
            }

            return granted;
        }

        /// <summary>The exclusion groups already standing in one ability's slots. Only that ability's
        /// own sockets are counted: the group says one of these at a time on one ability, not one of
        /// these in the whole build. Read off what the ability WEARS — an augment left in a closed slot
        /// does nothing, and blocking its group would charge the player for property he cannot use.</summary>
        private IReadOnlyCollection<string> WornGroups(string abilityId, string? excludingAddress = null)
        {
            HashSet<string> worn = new(StringComparer.Ordinal);

            foreach (AbilitySocket socket in SocketsOf(abilityId))
            {
                if (string.Equals(socket.Address, excludingAddress, StringComparison.Ordinal)) continue;
                if (socket.WorkingAugment is not { } installed) continue;

                string? group = augments?.Find(installed.AugmentId)?.ExclusionGroup;
                if (!string.IsNullOrWhiteSpace(group)) worn.Add(group);
            }

            return worn;
        }

        /// <summary>The augments sitting in the slots, each paired with the slot it sits in. Closed
        /// slots are in it because they are in the same collection: what the player owns is what the
        /// file writes, and a slot whose node he gave back still holds his augment.</summary>
        private IEnumerable<AbilitySocketOccupant> Installed()
        {
            foreach (AbilitySocket socket in _sockets.Values)
                if (socket.Augment is { } augment)
                    yield return new AbilitySocketOccupant(socket.Placement, augment);
        }
    }
}
