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
    /// What only this section knows: the slot layout per stance, the augments sitting in the ability
    /// sockets and the active stance. Neither the learned set nor the sockets themselves are stored —
    /// both follow from the passive-tree allocation, which is restored first
    /// (<see cref="RestoreOrder.PassiveTree"/> before <see cref="RestoreOrder.Abilities"/>).
    /// Storing them would give the book a second authority, and the two only agree until the tree's
    /// content moves: a node repointed at another ability would leave the file insisting on the
    /// ability the character no longer owns a node for. What each augment does carry is the slot it
    /// was chosen for — not to bring that slot back, but to recognise it: a socket Id whose ability or
    /// tier has moved names a different slot, and the augment does not follow the node into it.
    ///
    /// That restore order is also what makes the augments land at all. The tree section hands its
    /// allocation over first; the abilities are learned off it before this section runs, so by the time
    /// the saved arrangement is laid out there is a book to lay it onto — and the augments reach the
    /// abilities through the same binder a player's own install goes through
    /// (<see cref="IAbilityAugmentBinder"/>), never through a second road of the load's own.
    ///
    /// The saved layout wins over the auto-equip that learning performs, so an ability whose node was
    /// taken but which the file never placed stays out of the slots — the player's arrangement is his
    /// own, and a load must not rearrange it.
    ///
    /// The ornaments are the section's other half, and the one part of it that is neither derived from
    /// the tree nor rolled: which ability wears which ornament is a decision of the player's that nothing
    /// else in the game can reproduce. They go to the board in ONE call with the occupants — half a
    /// restored arrangement is a character whose fourth socket quietly went missing — and are laid down
    /// before the occupants land. An ornament the board will not take back goes to the BAG, and one that
    /// cannot even be minted stays in the file: a worn ornament exists nowhere else, and each of them comes
    /// from a quest that runs once, so no reading of the data may end with one gone.
    ///
    /// Version 2 dropped the learned list, version 3 added the sockets, version 4 made each of them
    /// carry the numbers its augment rolled, version 5 dropped the chosen upgrades (an ability is
    /// upgraded by exactly what stands in its sockets, so the second list had nothing left to say) and
    /// version 6 turned the socket map into a LIST, and version 7 added the ornaments. A file below 7
    /// simply names none, which is a character wearing none — the one migration that needs no code.
    /// The map was keyed by node, and a node repointed
    /// between builds can leave two entries under one id — the augment the player put in the slot it
    /// used to open and the one in the slot it opens now — so a map could only ever keep one of them.
    /// A version 4 or 5 file is still read: it carries every field the list carries, with the node id
    /// in the key instead of a field, and the choices a version 4 file names are reported as dropped
    /// rather than silently skipped. Nothing older: a version 3 entry names a
    /// record and no copy of it, and the only ways to finish reading one are to invent numbers the
    /// player never rolled or to hand him the record's bases — one is a different augment, the other is
    /// the tooltip lying about what is in the slot. Such a file is reported and the section is refused
    /// whole (<see cref="RestoreWithoutSection"/>), which is the one outcome that leaves the rest of the
    /// save alone: the file is a playthrough this build cannot dress, not a load that failed.
    /// </summary>
    /// <param name="sockets">Optional: a project composed without the battle module has no board,
    /// and the section is then written without its socket entries.</param>
    /// <param name="augments">Optional with the board: what carries the restored arrangement onto the
    /// abilities. A project holding no slots has nothing to carry.</param>
    /// <param name="copies">Optional, and read only for files written before a copy carried a rarity:
    /// those are drawn once at load, from the band their record declares now.</param>
    /// <param name="ornaments">Optional with the board: what an ornament id GRANTS. The file names the
    /// ornament and the ability and no tier, so the catalog is what turns an entry back into a socket —
    /// an ornament whose record left the game cannot be put back on and is reported rather than guessed
    /// at.</param>
    /// <param name="ornamentMinter">Optional: how an ornament this build cannot place becomes a thing the
    /// player can carry again. Without one the entry is kept in the file instead (see
    /// <see cref="Salvage"/>) — never dropped.</param>
    /// <param name="inventory">Optional: where a salvaged ornament goes. The bag is asked rather than
    /// assumed, because this section is also read by compositions that have none.</param>
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

        /// <summary>The property the socket entries live under. Read straight off the token rather than
        /// through the DTO: its SHAPE moved between versions, and a body deserialized whole would throw
        /// on the older one instead of migrating it.</summary>
        private const string SocketsProperty = "sockets";

        /// <summary>The property the ornament entries live under. Read straight off the token like the
        /// sockets, because they have to go back onto the abilities BEFORE the occupants land and the
        /// body's deserialization is skipped whenever there is no book to lay a layout onto.</summary>
        private const string OrnamentsProperty = "ornaments";

        /// <summary>
        /// Ornament entries this build could neither place nor hand over, kept so the next
        /// <see cref="Capture"/> writes them out again. The last resort of a rule with no exceptions:
        /// every ornament comes from one unrepeatable quest, so there is no state of the data in which
        /// forgetting one is the right answer. An id whose record was renamed between builds has no tier
        /// to grant and no item to mint, and this is the only thing left that does not spend it — the
        /// build that can read the id again finds it in the file.
        /// <para>
        /// Refilled from scratch by every restore and emptied by a file without the section, exactly like
        /// the board's own contents: this participant outlives the character, and one playthrough's
        /// unreadable ornament must not turn up in the next one's save.
        /// </para>
        /// </summary>
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
            // Before the body and before any guard that could return early: the sockets are the one part
            // of this section that carries the player's property, and a body this build could not make
            // sense of must not leave the board dressed in the playthrough before.
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

        /// <summary>
        /// The board is a singleton and outlives the scene, so a file that carries nothing for this
        /// section has to say so out loud: left alone, the slots would still hold the augments of the
        /// playthrough before. The layout does not need the same treatment — a book belongs to the
        /// player node, and the scene the load lands in builds a fresh one.
        /// </summary>
        public void RestoreWithoutSection()
        {
            _unplaceable.Clear();
            sockets?.Restore([], []);
            augments?.Bind();
        }

        /// <summary>
        /// Says out loud that the file carries a book this build cannot read, and lands on the state a
        /// file without the section leaves — the fresh-game one. Said rather than thrown: an exception
        /// would reach the same place through the manager's guard, but a version this build simply
        /// stopped supporting is a decision and not a failure, and reporting it here keeps the reason
        /// readable instead of turning it into a stack trace. The refusal is the whole section and
        /// nothing besides it: every other participant reads its own section untouched.
        /// </summary>
        private void Refuse(int savedVersion)
        {
            Tracker.TrackError(
                $"Save section '{SectionId}' was written at version {savedVersion}, which carries augment ids without " +
                $"the numbers each copy rolled. Version {OldestReadable} is the oldest that can be read; the section is dropped.",
                this);

            RestoreWithoutSection();
        }

        /// <summary>
        /// Says out loud that the file's free upgrade choices are not being read. They were a second
        /// authority over what an ability does, and the sockets are now the only one — so the choices
        /// go, and the player is told which abilities lost one instead of finding out by casting.
        /// Silent only where there is nothing to lose: a file that chose nothing says nothing.
        /// </summary>
        private void ReportRetiredChoices(JToken data, int savedVersion)
        {
            if (savedVersion >= Version || data[RetiredChoices] is not JObject chosen || chosen.Count == 0) return;

            Tracker.TrackError(
                $"Save section '{SectionId}' was written at version {savedVersion} and carries upgrade choices for " +
                $"{chosen.Count} ability(ies) ({string.Join(", ", chosen.Properties().Select(entry => entry.Name))}). " +
                "An ability now wears exactly the augments in its sockets, so the choices are dropped.",
                this);
        }

        /// <summary>Writes the occupied slots only, one entry each. An empty one carries nothing to
        /// remember, and the slot itself comes back with the node that opened it. Each augment goes down
        /// with the slot it was chosen for, so the load can tell that slot from another one the same
        /// node opens today — and with the numbers this copy rolled, which nothing else in the game can
        /// say again. A slot the allocation no longer backs is written like any other: it holds the
        /// player's property, and a file that forgot it would be a file that spent it.</summary>
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

        /// <summary>Writes which ability wears which ornament. An ornament with nothing in its socket is
        /// written like any other — the ornament itself is the property, and a file remembering it only
        /// while an augment sat in it would put the player's fourth socket back in his bag.</summary>
        private void CaptureOrnaments(AbilityBookSaveData data)
        {
            foreach (AbilityOrnament worn in sockets?.Ornaments ?? [])
                data.Ornaments.Add(new OrnamentSaveData { Ornament = worn.OrnamentId, Ability = worn.AbilityId });

            // The ones this build could not place and could not hand over. Written beside the worn ones
            // because the alternative is a save that spends them; none of them is on the board, so no
            // ornament is written twice.
            data.Ornaments.AddRange(_unplaceable);
        }

        /// <summary>
        /// Puts the ornaments back and makes sure every one of them ends up SOMEWHERE. There are three
        /// places an ornament can land and no fourth: on the ability the file names, in the bag, or — when
        /// neither is possible — back into the next save untouched. A worn ornament exists nowhere but in
        /// this section, and each comes from a quest that runs once, so a road that could drop one is a
        /// road that deletes content permanently.
        /// </summary>
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

                // No record, so no tier to grant and nothing to mint either — the one case the bag cannot
                // rescue. Inventing a tier would hand the player a socket nobody authored.
                Salvage(entry, ornaments == null
                    ? "this composition reads no ornament catalog"
                    : $"no record declares it (known: {string.Join(", ", ornaments.All.Select(known => known.Id))})");
            }

            foreach (AbilityOrnament displaced in sockets?.Restore(SavedOccupants(data), resolved) ?? [])
                Salvage(
                    new OrnamentSaveData { Ornament = displaced.OrnamentId, Ability = displaced.AbilityId },
                    $"'{displaced.AbilityId}' already wears another ornament, and an ability wears one");
        }

        /// <summary>
        /// Gets one ornament the board would not take back to the player: minted afresh and put in his
        /// bag, which is exactly what taking one off an ability does — an ornament rolls nothing, so a new
        /// copy IS the ornament. Where that cannot be done (no record to mint from, no bag, or no room in
        /// it) the entry is kept for the next save instead. Loud either way: the player keeps his artefact,
        /// and whoever moved the data hears which entry stopped fitting.
        /// </summary>
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

        /// <summary>The ornament entries the file carries, as written. Read off the raw token like the
        /// sockets: they are laid down before the body is deserialized, and the body's read is skipped
        /// whenever there is no book to lay a layout onto.</summary>
        private static IReadOnlyList<OrnamentSaveData> SavedOrnamentEntries(JToken data) =>
        [
            .. (data[OrnamentsProperty] as JArray ?? [])
                .Select(entry => entry.ToObject<OrnamentSaveData>())
                .Where(entry => entry != null)
                .Select(entry => entry!)
        ];

        /// <summary>
        /// The file's occupied slots in the board's own terms, read off the raw token because the shape
        /// of this one property moved: a list is today's, an object is what versions 4 and 5 wrote, and
        /// there the node id was the key. Anything else is a section with no sockets in it.
        /// </summary>
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

        /// <summary>One entry as the board holds it. The minter answers first, because it is the only
        /// thing here that can see a record and so the only thing that can draw what the file could not
        /// say — a rarity from before rarity was stored, an effect from before pools existed. It answers
        /// nothing for a record the catalog has dropped, and a file complete enough to stand on its own
        /// is then seated as written: an augment whose record left the game is still the player's.
        /// A legacy entry neither road can complete is reported rather than seated at a guessed rarity:
        /// a number invented here would be indistinguishable from one the player rolled.</summary>
        private AbilitySocketOccupant? Occupant(string socketId, SocketSaveData entry)
        {
            AugmentInstance? copy = copies?.Remembered(entry.Augment, entry.Values, entry.Rarity, entry.Effect)
                                    ?? (entry.Rarity is { } written
                                        // Without a minter there is no record to measure the written
                                        // effect against, so it is dropped rather than carried: a copy's
                                        // effect is a member of its record's pool on every road, and a
                                        // stale id kept here would be the one copy in the game holding
                                        // an effect nothing ever offered it.
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
