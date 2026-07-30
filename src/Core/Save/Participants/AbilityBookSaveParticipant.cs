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
    /// The learned set is mostly derived (AbilityUnlockService learns every non-hidden ability the
    /// book does not hold), but it is persisted anyway: future sources of abilities (items, seals)
    /// won't be derivable. What only this section knows: slot layout per stance, chosen upgrades
    /// (by stable Id) and the active stance.
    /// </summary>
    public class AbilityBookSaveParticipant(IPlayerAccessor playerAccessor, IAbilityProvider abilityProvider) : ISaveParticipant
    {
        public string SectionId => "abilityBook";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Abilities;

        public JToken Capture()
        {
            var book = playerAccessor.Player?.AbilityBook;
            if (book == null) return new JObject();

            var data = new AbilityBookSaveData { CurrentStance = book.CurrentStance };
            foreach (Stance stance in Enum.GetValues<Stance>())
                data.Stances[stance.ToString()] = new StanceBookSaveData
                {
                    Learned = [.. book.GetAbilities(stance).Select(ability => ability.Id)],
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
                RestoreLearned(book, stance, stanceData.Learned);
                RestoreSlots(book, stance, stanceData.Slots);
            }

            RestoreUpgrades(book, saved.Upgrades);
            book.SetStance(saved.CurrentStance);
        }

        private void RestoreLearned(IAbilityBookComponent book, Stance stance, List<string> learnedIds)
        {
            // Loading an earlier save in the same session: extra abilities must be forgotten
            var savedIds = learnedIds.ToHashSet();
            foreach (var extra in book.GetAbilities(stance).Where(a => !savedIds.Contains(a.Id)).ToList())
                book.Forget(extra.InstanceId);

            var alreadyLearned = book.GetAbilities(stance).Select(ability => ability.Id).ToHashSet();
            foreach (string abilityId in learnedIds.Where(id => !alreadyLearned.Contains(id)))
            {
                if (!abilityProvider.KnownAbilityIds.Contains(abilityId)) continue; // ability removed from the data
                book.Learn(stance, abilityProvider.CreateAbility(abilityId));
            }
        }

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
