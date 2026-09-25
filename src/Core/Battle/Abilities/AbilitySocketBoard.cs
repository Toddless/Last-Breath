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

        /// <summary>The ornaments worn, by the ornament's own id. Keyed by the ornament and not by the
        /// ability because an ornament is a THING and there is one of each in the game: the id is what
        /// the bag hands over, what the file writes and what the player moves.</summary>
        private readonly Dictionary<string, AbilityOrnament> _ornaments = new(StringComparer.Ordinal);

        public event Action? Changed;

        public IReadOnlyList<AbilitySocket> Sockets => [.. _sockets.Values];

        public IReadOnlyCollection<AbilitySocketOccupant> Occupants => [.. Installed()];

        public IReadOnlyCollection<AbilityOrnament> Ornaments => [.. _ornaments.Values];

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

        public AbilityOrnament? OrnamentOn(string abilityId)
        {
            foreach (AbilityOrnament worn in _ornaments.Values)
                if (string.Equals(worn.AbilityId, abilityId, StringComparison.Ordinal)) return worn;

            return null;
        }

        public AbilityOrnament? Attachment(string ornamentId) =>
            _ornaments.TryGetValue(ornamentId, out AbilityOrnament worn) ? worn : null;

        public bool Attach(AbilityOrnament ornament)
        {
            if (_ornaments.ContainsKey(ornament.OrnamentId) || OrnamentOn(ornament.AbilityId) != null) return false;

            _ornaments[ornament.OrnamentId] = ornament;
            Open(ornament.Slot);
            Changed?.Invoke();
            return true;
        }

        public bool Detach(string ornamentId)
        {
            if (!_ornaments.TryGetValue(ornamentId, out AbilityOrnament worn)) return false;

            // Remove-only, exactly as a closed slot is: the augment comes out first, and by its own
            // road. Otherwise an ornament carried to an ability with other tags would take an augment
            // with it that the fitting rule never agreed to put there.
            if (Find(worn.Slot.Address) is { IsEmpty: false }) return false;

            _ornaments.Remove(ornamentId);
            _sockets.Remove(worn.Slot.Address);
            Changed?.Invoke();
            return true;
        }

        public void Sync(IReadOnlyCollection<AbilitySocketPlacement> open)
        {
            bool moved = false;
            HashSet<string> opened = new(StringComparer.Ordinal);

            // The ornaments' slots stand beside the allocation's in one set, which is what makes a
            // respec unable to touch them: retirement is what the set does NOT name, and an ornament
            // names its own slot on every pass, whatever the tree says that pass.
            foreach (AbilitySocketPlacement placement in open.Concat(Granted()))
            {
                opened.Add(placement.Address);
                moved |= Open(placement);
            }

            foreach (string address in _sockets.Keys.Where(address => !opened.Contains(address)).ToList())
                moved |= Retire(address);

            if (moved) Changed?.Invoke();
        }

        public IReadOnlyCollection<AbilityOrnament> Restore(
            IReadOnlyCollection<AbilitySocketOccupant> saved,
            IReadOnlyCollection<AbilityOrnament> ornaments)
        {
            // The ornaments of the playthrough being left behind go with their slots, and before the
            // purge below rather than after: their slots are OPEN, so nothing else would drop them.
            foreach (AbilityOrnament worn in _ornaments.Values) _sockets.Remove(worn.Slot.Address);
            _ornaments.Clear();

            // The closed slots of the playthrough being left behind go with it: the board is a
            // singleton, and a slot held open by another character's augment would be written into
            // this one's first save.
            foreach (string address in _sockets.Where(entry => !entry.Value.IsOpen).Select(entry => entry.Key).ToList())
                _sockets.Remove(address);
            foreach (AbilitySocket socket in _sockets.Values) socket.Extract();

            // Before the occupants, so that an augment written into an ornament's slot lands in a slot
            // that already exists. Not a guarantee resting on the order: an entry reaching an address
            // nothing stands at brings its own slot back closed, and the attach behind it REOPENS that
            // address (see Open), so the augment ends up live either way. The order is which of the two
            // is the cause — an ornament opens a slot, an entry fills one.
            IReadOnlyCollection<AbilityOrnament> displaced = Wear(ornaments);

            foreach (AbilitySocketOccupant occupant in saved) Seat(occupant);

            // Judged only after every occupant is back: a grantee's fit depends on the donors beside
            // it, and reporting mid-restore would let the order of entries in the file pick the verdict.
            foreach (AbilitySocketOccupant occupant in saved) ReportIllFitting(occupant);

            Changed?.Invoke();
            return displaced;
        }

        /// <summary>Back to the board of a fresh character: no slots at all. The sockets themselves come
        /// back from the allocation, so a tree that can be read fills the board again through its own
        /// reset — and a launch whose document never parsed would otherwise carry the closed slots of
        /// the file it loaded into the playthrough that follows.</summary>
        public void ResetSession()
        {
            if (_sockets.Count == 0 && _ornaments.Count == 0) return;

            _sockets.Clear();
            // Ornaments are the board's own, so nothing else would clear them: a fresh character would
            // otherwise start the game already wearing the previous one's fourth socket.
            _ornaments.Clear();
            Changed?.Invoke();
        }

        /// <summary>The slots the worn ornaments grant. Read on every <see cref="Sync"/> beside the
        /// allocation's, because that is what "a respec does not touch the ornaments" means in one
        /// place instead of a case inside retirement.</summary>
        private IEnumerable<AbilitySocketPlacement> Granted() => _ornaments.Values.Select(worn => worn.Slot);

        /// <summary>Makes one placement a live slot: a fresh one where nothing stands, and the same slot
        /// woken up where something closed does. True when anything changed.</summary>
        private bool Open(AbilitySocketPlacement placement)
        {
            if (!_sockets.TryGetValue(placement.Address, out AbilitySocket? standing))
            {
                _sockets[placement.Address] = new AbilitySocket(placement.SocketId, placement.AbilityId, placement.Tier);
                return true;
            }

            // The same signature is the same slot: a node bought back is the place the augment was
            // sitting in, so the slot wakes up around whatever is still in it.
            if (standing.IsOpen) return false;

            standing.Reopen();
            return true;
        }

        /// <summary>
        /// Puts the saved ornaments back on their abilities and hands back the ones that could not go on
        /// — DISPLACED artefacts, each of which is now nowhere and is the caller's to put somewhere.
        /// Handing them back rather than reporting them is the whole point: an ornament comes from one
        /// unrepeatable quest, so the board must not be the last thing that ever held it.
        /// <para>
        /// One case is deliberately NOT handed back: a file naming the same ornament twice. That one IS
        /// on an ability — the first entry put it there — so nothing was displaced, and returning it
        /// would mint a second of an artefact there is one of in the game. That is said out loud and
        /// dropped, which costs the player nothing.
        /// </para>
        /// </summary>
        private IReadOnlyCollection<AbilityOrnament> Wear(IReadOnlyCollection<AbilityOrnament> ornaments)
        {
            List<AbilityOrnament> displaced = [];

            foreach (AbilityOrnament ornament in ornaments)
            {
                if (Attach(ornament)) continue;

                if (Attachment(ornament.OrnamentId) is { } standing)
                {
                    Tracker.TrackError(
                        $"Saved ornaments name '{ornament.OrnamentId}' twice; it is on '{standing.AbilityId}', " +
                        $"so the entry putting it on '{ornament.AbilityId}' is dropped rather than handed back — " +
                        "handing it back would make a second of an artefact there is one of.",
                        this);
                    continue;
                }

                displaced.Add(ornament);
            }

            return displaced;
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
        /// nothing. Asked of the COPY and not of its record, which is the one place the two differ:
        /// a record offering a pool of effects grants the genus of the one THIS copy drew, so two
        /// copies of it teach the ability different things.</summary>
        private IReadOnlyCollection<string> GrantedTags(string abilityId, string? excludingAddress = null)
        {
            HashSet<string> granted = new(StringComparer.OrdinalIgnoreCase);

            foreach (AbilitySocket socket in SocketsOf(abilityId))
            {
                if (string.Equals(socket.Address, excludingAddress, StringComparison.Ordinal)) continue;
                if (socket.WorkingAugment is not { } installed) continue;
                if (augments?.Find(installed.AugmentId) is not { } record) continue;

                foreach (string tag in installed.Applied(record).GrantsTags) granted.Add(tag);
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
