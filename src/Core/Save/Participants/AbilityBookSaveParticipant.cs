namespace Core.Save.Participants
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Data.SaveData;
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
    /// Version 2 dropped the learned list, version 3 added the sockets, version 4 made each of them
    /// carry the numbers its augment rolled, version 5 dropped the chosen upgrades (an ability is
    /// upgraded by exactly what stands in its sockets, so the second list had nothing left to say) and
    /// version 6 turned the socket map into a LIST. The map was keyed by node, and a node repointed
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
    public class AbilityBookSaveParticipant(
        IPlayerAccessor playerAccessor,
        IAbilitySocketBoard? sockets = null,
        IAbilityAugmentBinder? augments = null,
        IAugmentItemMinter? copies = null)
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

        public string SectionId => "abilityBook";
        public int Version => 6;
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
            sockets?.Restore(SavedOccupants(data));
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
            sockets?.Restore([]);
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
                    Ability = occupant.Slot.AbilityId,
                    Tier = occupant.Slot.Tier
                });
        }

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

        /// <summary>One entry as the board holds it. A file that says what the copy is worth is put back
        /// exactly as written; one from before rarity was stored has to be DRAWN, and the draw belongs to
        /// the minter — the record's band is what it must land in, and nothing here can see a record.
        /// A legacy entry with no minter to draw it is reported rather than seated at a guessed rarity:
        /// a number invented here would be indistinguishable from one the player rolled.</summary>
        private AbilitySocketOccupant? Occupant(string socketId, SocketSaveData entry)
        {
            AugmentInstance? copy = entry.Rarity is { } written
                ? new AugmentInstance(entry.Augment, entry.Values, written)
                : copies?.Remembered(entry.Augment, entry.Values, null);

            if (copy != null)
                return new AbilitySocketOccupant(new AbilitySocketPlacement(socketId, entry.Ability, entry.Tier), copy);

            Tracker.TrackError(
                $"Socket '{socketId}' holds '{entry.Augment}' written before copies carried a rarity, and this " +
                "composition has no augment minter to draw one: the slot comes back empty.",
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
