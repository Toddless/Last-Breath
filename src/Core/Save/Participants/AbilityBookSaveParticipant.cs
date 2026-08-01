namespace Core.Save.Participants
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.SaveData;
    using Entity.Components;
    using Enums;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>
    /// What only this section knows: the slot layout per stance, the chosen upgrades (by stable Id)
    /// and the active stance. The learned set itself is not stored — it follows from the passive-tree
    /// allocation, which is restored first (<see cref="RestoreOrder.PassiveTree"/> before
    /// <see cref="RestoreOrder.Abilities"/>). Storing it would give the book a second authority, and
    /// the two only agree until the tree's content moves: a node repointed at another ability would
    /// leave the file insisting on the ability the character no longer owns a node for.
    ///
    /// The saved layout wins over the auto-equip that learning performs, so an ability whose node was
    /// taken but which the file never placed stays out of the slots — the player's arrangement is his
    /// own, and a load must not rearrange it.
    ///
    /// Version 2 dropped the learned list; a version 1 file still restores, its list simply unread.
    /// </summary>
    public class AbilityBookSaveParticipant(IPlayerAccessor playerAccessor) : ISaveParticipant
    {
        public string SectionId => "abilityBook";
        public int Version => 2;
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

            return JToken.FromObject(data);
        }

        public void Restore(JToken data, int savedVersion)
        {
            var book = playerAccessor.Player?.AbilityBook;
            var saved = data.ToObject<AbilityBookSaveData>();
            if (book == null || saved == null) return;

            foreach ((string stanceName, StanceBookSaveData stanceData) in saved.Stances)
            {
                if (!Enum.TryParse(stanceName, out Stance stance)) continue; // stance removed from the game
                RestoreSlots(book, stance, stanceData.Slots);
            }

            RestoreUpgrades(book, saved.Upgrades);
            book.SetStance(saved.CurrentStance);
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
