namespace Core.Save.Participants
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Data.SaveData;
    using Entity.Components;
    using Enums;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>
    /// What only this section knows: the slot layout per stance, the chosen upgrades (by stable Id),
    /// the augments sitting in the ability sockets and the active stance. Neither the learned set nor
    /// the sockets themselves are stored — both follow from the passive-tree allocation, which is
    /// restored first (<see cref="RestoreOrder.PassiveTree"/> before <see cref="RestoreOrder.Abilities"/>).
    /// Storing them would give the book a second authority, and the two only agree until the tree's
    /// content moves: a node repointed at another ability would leave the file insisting on the
    /// ability the character no longer owns a node for. What each augment does carry is the slot it
    /// was chosen for — not to bring that slot back, but to recognise it: a socket Id whose ability or
    /// tier has moved names a different slot, and the augment does not follow the node into it.
    ///
    /// The saved layout wins over the auto-equip that learning performs, so an ability whose node was
    /// taken but which the file never placed stays out of the slots — the player's arrangement is his
    /// own, and a load must not rearrange it.
    ///
    /// Version 2 dropped the learned list, version 3 added the sockets, and version 4 made each of them
    /// carry the numbers its augment rolled. Nothing older is read: a version 3 entry names a record and
    /// no copy of it, and the only ways to finish reading one are to invent numbers the player never
    /// rolled or to hand him the record's bases — one is a different augment, the other is the tooltip
    /// lying about what is in the slot. An older file is reported and the section is refused whole
    /// (<see cref="RestoreWithoutSection"/>), which is the one outcome that leaves the rest of the save
    /// alone: the file is a playthrough this build cannot dress, not a load that failed.
    /// </summary>
    /// <param name="sockets">Optional: a project composed without the battle module has no board,
    /// and the section is then written without its socket entries.</param>
    public class AbilityBookSaveParticipant(IPlayerAccessor playerAccessor, IAbilitySocketBoard? sockets = null)
        : ISaveParticipant
    {
        public string SectionId => "abilityBook";
        public int Version => 4;
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

            foreach (var ability in book.AllAbilities.Where(a => a.CurrentUpgrades.Count > 0))
                data.Upgrades[ability.Id] = ability.CurrentUpgrades.ToDictionary(pair => pair.Key, pair => pair.Value.Id);

            CaptureSockets(data);
            return JToken.FromObject(data);
        }

        public void Restore(JToken data, int savedVersion)
        {
            if (savedVersion < Version)
            {
                Refuse(savedVersion);
                return;
            }

            var book = playerAccessor.Player?.AbilityBook;
            var saved = data.ToObject<AbilityBookSaveData>();
            if (saved == null) return;

            sockets?.Restore(SavedOccupants(saved));
            if (book == null) return;

            foreach ((string stanceName, StanceBookSaveData stanceData) in saved.Stances)
            {
                if (!Enum.TryParse(stanceName, out Stance stance)) continue; // stance removed from the game
                RestoreSlots(book, stance, stanceData.Slots);
            }

            RestoreUpgrades(book, saved.Upgrades);
            book.SetStance(saved.CurrentStance);
        }

        /// <summary>
        /// The board is a singleton and outlives the scene, so a file that carries nothing for this
        /// section has to say so out loud: left alone, the slots would still hold the augments of the
        /// playthrough before. The layout does not need the same treatment — a book belongs to the
        /// player node, and the scene the load lands in builds a fresh one.
        /// </summary>
        public void RestoreWithoutSection() => sockets?.Restore([]);

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
                $"the numbers each copy rolled. Version {Version} is the oldest that can be read; the section is dropped.",
                this);

            RestoreWithoutSection();
        }

        /// <summary>Writes the occupied slots only. An empty one carries nothing to remember, and the
        /// slot itself comes back with the node that opened it. Each augment goes down with the slot
        /// it was chosen for, so the load can tell that slot from another one wearing the same id —
        /// and with the numbers this copy rolled, which nothing else in the game can say again.</summary>
        private void CaptureSockets(AbilityBookSaveData data)
        {
            foreach (AbilitySocketOccupant occupant in sockets?.Occupants ?? [])
                data.Sockets[occupant.Slot.SocketId] = new SocketSaveData
                {
                    Augment = occupant.Augment.AugmentId,
                    Values = new Dictionary<string, float>(occupant.Augment.Values),
                    Ability = occupant.Slot.AbilityId,
                    Tier = occupant.Slot.Tier
                };
        }

        /// <summary>The file's occupied slots in the board's own terms.</summary>
        private static IReadOnlyCollection<AbilitySocketOccupant> SavedOccupants(AbilityBookSaveData saved) =>
        [
            .. saved.Sockets.Select(entry => new AbilitySocketOccupant(
                new AbilitySocketPlacement(entry.Key, entry.Value.Ability, entry.Value.Tier),
                new AugmentInstance(entry.Value.Augment, entry.Value.Values)))
        ];

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

        private static void RestoreUpgrades(IAbilityBookComponent book, Dictionary<string, Dictionary<int, string>> savedUpgrades)
        {
            foreach ((string abilityId, Dictionary<int, string> tiers) in savedUpgrades)
            {
                var ability = book.AllAbilities.FirstOrDefault(a => a.Id == abilityId);
                if (ability == null) continue;
                foreach ((int tier, string upgradeId) in tiers)
                    ability.SelectUpgrade(tier, upgradeId);
            }
        }
    }
}
