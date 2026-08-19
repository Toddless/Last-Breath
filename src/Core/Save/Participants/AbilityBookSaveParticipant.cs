namespace Core.Save.Participants
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Data.SaveData;
    using Inventory;
    using Items;
    using Entity.Components;
    using Enums;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>
    /// The slot layout per stance, the augments sitting in the ability sockets and the active stance.
    /// Neither the learned set nor the sockets themselves are stored: both follow from the passive-tree
    /// allocation, restored first (<see cref="RestoreOrder.PassiveTree"/> before
    /// <see cref="RestoreOrder.Abilities"/>), so there is a book to lay the arrangement onto and no second
    /// authority over which abilities the character owns. Each augment does carry the slot it was chosen
    /// for — to recognise that slot, not to recreate it: a socket id whose ability or tier moved names a
    /// different slot, and the augment does not follow the node into it. The augments reach the abilities
    /// through the same binder a player's own install goes through (<see cref="IAbilityAugmentBinder"/>).
    /// <para>The saved layout wins over the auto-equip that learning performs — a load must not rearrange
    /// the player's own arrangement.</para>
    /// <para>The ornaments are the one part of the section neither derived from the tree nor rolled. They
    /// go to the board in ONE call with the occupants and are laid down first — half a restored arrangement
    /// is a character whose fourth socket quietly went missing; one the board will not take back goes to
    /// the BAG, and one that cannot even be minted stays in the file. Each comes from a quest
    /// that runs once and exists nowhere else, so no reading of the data may end with one gone.</para>
    /// <para>Versions: 2 dropped the learned list, 3 added the sockets, 4 gave each the numbers its augment
    /// rolled, 5 dropped the free upgrade choices (the sockets are now the only authority over what an
    /// ability does), 6 turned the socket map into a LIST because a repointed node can leave two entries
    /// under one node id and a map could keep only one, 7 added the ornaments (an older file names none,
    /// which is a character wearing none). Files 4 and 5 are still read, node id in the key instead of a
    /// field. Below <see cref="OldestReadable"/> the section is refused whole
    /// (<see cref="RestoreWithoutSection"/>), leaving every other section untouched.</para>
    /// </summary>
    /// <param name="sockets">Optional: a composition without the battle module has no board, and the
    /// section is then written without its socket entries.</param>
    /// <param name="augments">Optional with the board: what carries the restored arrangement onto the
    /// abilities.</param>
    /// <param name="copies">Optional, and read only for files written before a copy carried a rarity:
    /// those are drawn once at load, from the band their record declares now.</param>
    /// <param name="ornaments">Optional with the board: what an ornament id GRANTS. The file names no tier,
    /// so an ornament whose record left the game is reported rather than guessed at.</param>
    /// <param name="ornamentMinter">Optional: how an ornament this build cannot place becomes a carryable
    /// item. Without one the entry is kept in the file instead (see <see cref="Salvage"/>).</param>
    /// <param name="inventory">Optional: where a salvaged ornament goes. Asked rather than assumed,
    /// because compositions without a bag also read this section.</param>
    public class AbilityBookSaveParticipant(
        IPlayerAccessor playerAccessor,
        IAbilitySocketBoard? sockets = null,
        IAbilityAugmentBinder? augments = null,
        IAugmentItemMinter? copies = null,
        IOrnamentCatalog? ornaments = null,
        IOrnamentMinter? ornamentMinter = null,
        IInventory? inventory = null)
        : ISaveParticipant
    {
        /// <summary>The oldest section this build can still dress a character from. Below it the file
        /// carries augment ids without the numbers each copy rolled, which is not a book at all.</summary>
        private const int OldestReadable = 4;

        /// <summary>The property a version 4 file kept its free upgrade choices under.</summary>
        private const string RetiredChoices = "upgrades";

        /// <summary>The property the socket entries live under. Read straight off the token: its SHAPE
        /// moved between versions, and a body deserialized whole would throw instead of migrating.</summary>
        private const string SocketsProperty = "sockets";

        /// <summary>The property the ornament entries live under. Read off the token like the sockets: they
        /// land before the occupants, and the body's read is skipped when there is no book.</summary>
        private const string OrnamentsProperty = "ornaments";

        /// <summary>Ornament entries this build could neither place nor hand over, kept so the next
        /// <see cref="Capture"/> writes them out again and a build that can read the id finds them.
        /// Refilled from scratch by every restore and emptied by a file without the section: this
        /// participant outlives the character, and one playthrough's entry must not reach the next
        /// one's save.</summary>
        private readonly List<OrnamentSaveData> _unplaceable = [];

        public string SectionId => "abilityBook";
        public int Version => 7;
        public int RestoreOrder => Save.RestoreOrder.Abilities;

        public JToken Capture()
        {
            var book = playerAccessor.Player?.AbilityBook;
            if (book == null) return new JObject();

            var data = new AbilityBookSaveData { CurrentStance = book.CurrentStance };
            foreach (Stance stance in Enum.GetValues<Stance>())
                data.Stances[stance.ToString()] = new StanceBookSaveData
                {
                    Slots = [.. book.GetSlotLayout(stance).Select(ability => ability?.Id)]
                };

            CaptureSockets(data);
            CaptureOrnaments(data);
            return JToken.FromObject(data);
        }

        public void Restore(JToken data, int savedVersion)
        {
            if (savedVersion < OldestReadable)
            {
                Refuse(savedVersion);
                return;
            }

            ReportRetiredChoices(data, savedVersion);
            // Before the body and before any guard that could return early: the sockets carry the player's
            // property, and an unreadable body must not leave the board dressed in the previous playthrough.
            RestoreOrnaments(data);
            RestoreLayout(data);

            // After the slots and whether or not there is a book: the binder is what turns the restored
            // arrangement into upgrades, and it is the same one a player's own install goes through.
            augments?.Bind();
        }

        /// <summary>Puts the stances back the way the file left them. Skipped whole where there is no
        /// book to lay them onto, or where the body says nothing this build can read — neither costs the
        /// player anything, because the slots were restored before this ran.</summary>
        private void RestoreLayout(JToken data)
        {
            var book = playerAccessor.Player?.AbilityBook;
            if (book == null) return;

            var saved = Body(data)?.ToObject<AbilityBookSaveData>();
            if (saved == null) return;

            foreach ((string stanceName, StanceBookSaveData stanceData) in saved.Stances)
            {
                if (!Enum.TryParse(stanceName, out Stance stance)) continue; // stance removed from the game
                RestoreSlots(book, stance, stanceData.Slots);
            }

            book.SetStance(saved.CurrentStance);
        }

        /// <summary>The section without its socket entries. They are read on their own, by shape, and
        /// leaving them in would make the whole body unreadable on a file that wrote them as a map.</summary>
        private static JToken Body(JToken data)
        {
            if (data is not JObject section) return data;

            var body = (JObject)section.DeepClone();
            body.Remove(SocketsProperty);
            return body;
        }

        /// <summary>Clears the board explicitly: it is a singleton outliving the scene, so a file with
        /// nothing for this section would otherwise leave the slots holding the previous playthrough's
        /// augments. The layout needs no such pass — the scene builds a fresh book.</summary>
        public void RestoreWithoutSection()
        {
            _unplaceable.Clear();
            sockets?.Restore([], []);
            augments?.Bind();
        }

        /// <summary>Reports a book this build cannot read and lands on the fresh-game state. Reported
        /// rather than thrown — an unsupported version is a decision, not a failure — and the refusal
        /// covers this section only: every other participant reads its own untouched.</summary>
        private void Refuse(int savedVersion)
        {
            Tracker.TrackError(
                $"Save section '{SectionId}' was written at version {savedVersion}, which carries augment ids without " +
                $"the numbers each copy rolled. Version {OldestReadable} is the oldest that can be read; the section is dropped.",
                this);

            RestoreWithoutSection();
        }

        /// <summary>Reports the free upgrade choices an old file carries as dropped — the sockets are now
        /// the only authority — so the player is told instead of finding out by casting. Silent when the
        /// file chose nothing.</summary>
        private void ReportRetiredChoices(JToken data, int savedVersion)
        {
            if (savedVersion >= Version || data[RetiredChoices] is not JObject chosen || chosen.Count == 0) return;

            Tracker.TrackError(
                $"Save section '{SectionId}' was written at version {savedVersion} and carries upgrade choices for " +
                $"{chosen.Count} ability(ies) ({string.Join(", ", chosen.Properties().Select(entry => entry.Name))}). " +
                "An ability now wears exactly the augments in its sockets, so the choices are dropped.",
                this);
        }

        /// <summary>Writes the occupied slots only — an empty one comes back with its node. Each entry
        /// carries the slot the augment was chosen for and the numbers this copy rolled, which nothing else
        /// can say again. A slot the allocation no longer backs is written like any other: forgetting it
        /// would spend the player's property.</summary>
        private void CaptureSockets(AbilityBookSaveData data)
        {
            foreach (AbilitySocketOccupant occupant in sockets?.Occupants ?? [])
                data.Sockets.Add(new SocketSaveData
                {
                    Socket = occupant.Slot.SocketId,
                    Augment = occupant.Augment.AugmentId,
                    Values = new Dictionary<string, float>(occupant.Augment.Values),
                    Rarity = occupant.Augment.Rarity,
                    Effect = occupant.Augment.EffectId,
                    Ability = occupant.Slot.AbilityId,
                    Tier = occupant.Slot.Tier
                });
        }

        /// <summary>Writes which ability wears which ornament, whether or not its socket holds anything:
        /// the ornament itself is the property.</summary>
        private void CaptureOrnaments(AbilityBookSaveData data)
        {
            foreach (AbilityOrnament worn in sockets?.Ornaments ?? [])
                data.Ornaments.Add(new OrnamentSaveData { Ornament = worn.OrnamentId, Ability = worn.AbilityId });

            // The ones this build could place nowhere: written beside the worn ones so the save does not
            // spend them. None is on the board, so no ornament is written twice.
            data.Ornaments.AddRange(_unplaceable);
        }

        /// <summary>Puts the ornaments back, and every one lands SOMEWHERE: on the ability the file names,
        /// in the bag, or back into the next save untouched. There is no fourth place — dropping one would
        /// delete content that exists nowhere else.</summary>
        private void RestoreOrnaments(JToken data)
        {
            _unplaceable.Clear();

            List<AbilityOrnament> resolved = [];
            foreach (OrnamentSaveData entry in SavedOrnamentEntries(data))
            {
                if (ornaments?.Find(entry.Ornament) is { } record)
                {
                    resolved.Add(new AbilityOrnament(entry.Ornament, entry.Ability, record.Tier));
                    continue;
                }

                // No record: no tier to grant and nothing to mint. Inventing a tier would hand the player
                // a socket nobody authored.
                Salvage(entry, ornaments == null
                    ? "this composition reads no ornament catalog"
                    : $"no record declares it (known: {string.Join(", ", ornaments.All.Select(known => known.Id))})");
            }

            foreach (AbilityOrnament displaced in sockets?.Restore(SavedOccupants(data), resolved) ?? [])
                Salvage(
                    new OrnamentSaveData { Ornament = displaced.OrnamentId, Ability = displaced.AbilityId },
                    $"'{displaced.AbilityId}' already wears another ornament, and an ability wears one");
        }

        /// <summary>Gets one ornament the board would not take back to the player: minted afresh into his
        /// bag — an ornament rolls nothing, so a new copy IS the ornament. With no record, no bag or no room
        /// the entry is kept for the next save instead. Reported either way.</summary>
        private void Salvage(OrnamentSaveData entry, string reason)
        {
            if (ornamentMinter?.Mint(entry.Ornament) is { } item && inventory?.TryAddItem(item) == true)
            {
                Tracker.TrackError(
                    $"Ornament '{entry.Ornament}' cannot go back onto '{entry.Ability}' ({reason}); " +
                    "it is in the bag instead, to be put wherever the player wants it.",
                    this);
                return;
            }

            _unplaceable.Add(entry);
            Tracker.TrackError(
                $"Ornament '{entry.Ornament}' cannot go back onto '{entry.Ability}' ({reason}) and cannot be " +
                "carried either (nothing to mint it from, or no room for it); the entry is written out again " +
                "unchanged, so a build that can place it will.",
                this);
        }

        /// <summary>The ornament entries the file carries, as written.</summary>
        private static IReadOnlyList<OrnamentSaveData> SavedOrnamentEntries(JToken data) =>
        [
            .. (data[OrnamentsProperty] as JArray ?? [])
                .Select(entry => entry.ToObject<OrnamentSaveData>())
                .Where(entry => entry != null)
                .Select(entry => entry!)
        ];

        /// <summary>The file's occupied slots in the board's own terms. A list is today's shape, an object
        /// is what versions 4 and 5 wrote with the node id as key; anything else means no sockets.</summary>
        private IReadOnlyCollection<AbilitySocketOccupant> SavedOccupants(JToken data) =>
            data[SocketsProperty] switch
            {
                JArray entries =>
                [
                    .. entries
                        .Select(entry => entry.ToObject<SocketSaveData>())
                        .Where(entry => entry != null)
                        .Select(entry => Occupant(entry!.Socket, entry))
                        .Where(occupant => occupant.HasValue)
                        .Select(occupant => occupant!.Value)
                ],
                JObject keyed =>
                [
                    .. keyed.Properties()
                        .Select(property => (property.Name, Entry: property.Value.ToObject<SocketSaveData>()))
                        .Where(pair => pair.Entry != null)
                        .Select(pair => Occupant(pair.Name, pair.Entry!))
                        .Where(occupant => occupant.HasValue)
                        .Select(occupant => occupant!.Value)
                ],
                _ => []
            };

        /// <summary>One entry as the board holds it. The minter answers first — it is the only thing here
        /// that sees a record and can draw what an old file left unsaid — and an entry complete on its own
        /// is seated as written, so an augment whose record left the game is still the player's. A legacy
        /// entry neither road completes is reported rather than seated at a guessed rarity, which would be
        /// indistinguishable from one the player rolled.</summary>
        private AbilitySocketOccupant? Occupant(string socketId, SocketSaveData entry)
        {
            AugmentInstance? copy = copies?.Remembered(entry.Augment, entry.Values, entry.Rarity, entry.Effect)
                                    ?? (entry.Rarity is { } written
                                        // The written effect is dropped without a minter: there is no
                                        // record to measure it against, and a copy's effect must be a
                                        // member of its record's pool on every road.
                                        ? new AugmentInstance(entry.Augment, entry.Values, written)
                                        : null);

            if (copy != null)
                return new AbilitySocketOccupant(new AbilitySocketPlacement(socketId, entry.Ability, entry.Tier), copy);

            Tracker.TrackError(
                $"Socket '{socketId}' holds '{entry.Augment}' written before copies carried a rarity, and no " +
                "record is left to draw one from: the slot comes back empty.",
                this);
            return null;
        }

        /// <summary>Lays the slots out as the file left them. A slot naming an ability the book does
        /// not hold stays empty: the abilities are the allocation's to give, and the file's memory of
        /// one it no longer grants cannot conjure it.</summary>
        private static void RestoreSlots(IAbilityBookComponent book, Stance stance, List<string?> savedSlots)
        {
            var learned = book.GetAbilities(stance);
            int slotCount = book.GetSlotLayout(stance).Count;
            for (int slot = 0; slot < slotCount; slot++)
                book.Unequip(stance, slot);

            for (int slot = 0; slot < Math.Min(savedSlots.Count, slotCount); slot++)
            {
                string? abilityId = savedSlots[slot];
                if (abilityId == null) continue;
                var ability = learned.FirstOrDefault(a => a.Id == abilityId);
                if (ability != null) book.Equip(stance, ability.InstanceId, slot);
            }
        }
    }
}
