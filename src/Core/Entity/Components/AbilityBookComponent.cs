namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Constants;
    using Entity;
    using Enums;

    public class AbilityBookComponent(
        IFightable owner,
        int slotsPerStance = BattleConstants.AbilitySlotsPerStance,
        Stance initialStance = Stance.Dexterity) : IAbilityBookComponent
    {
        private readonly Dictionary<Stance, List<IAbility>> _learnedByStance = [];
        private readonly Dictionary<Stance, IAbility?[]> _slotsByStance = [];

        public Stance CurrentStance { get; private set; } = initialStance;
        public IReadOnlyList<IAbility> AllAbilities => _learnedByStance.Values.SelectMany(list => list).ToList();
        public IReadOnlyList<IAbility> ActiveAbilities => GetSlots(CurrentStance).OfType<IAbility>().ToList();
        public IReadOnlyList<IAbility?> ActiveSlots => GetSlots(CurrentStance);

        public event Action<IAbility>? AbilityLearned;
        public event Action<IAbility>? AbilityForgotten;
        public event Action? ActiveAbilitiesChanged;

        public IReadOnlyList<IAbility> GetAbilities(Stance stance) => GetLearned(stance);

        public IReadOnlyList<IAbility?> GetSlotLayout(Stance stance) => GetSlots(stance);

        public void Learn(Stance stance, IAbility ability)
        {
            GetLearned(stance).Add(ability);
            ability.SetOwner(owner);
            EquipToFirstFreeSlot(stance, ability);
            AbilityLearned?.Invoke(ability);
        }

        public void Forget(string instanceId)
        {
            foreach ((Stance stance, List<IAbility> learned) in _learnedByStance)
            {
                var ability = learned.FirstOrDefault(a => a.IsSame(instanceId));
                if (ability == null) continue;

                learned.Remove(ability);
                ClearSlotsOf(stance, ability);
                ability.RemoveOwner();
                AbilityForgotten?.Invoke(ability);
                NotifyIfActiveStance(stance);
                return;
            }
        }

        public void Equip(Stance stance, string instanceId, int slot)
        {
            var ability = GetLearned(stance).FirstOrDefault(a => a.IsSame(instanceId));
            if (ability == null || !IsValidSlot(slot)) return;

            ClearSlotsOf(stance, ability); // moving between slots must not leave a duplicate behind
            GetSlots(stance)[slot] = ability;
            NotifyIfActiveStance(stance);
        }

        public void Unequip(Stance stance, int slot)
        {
            if (!IsValidSlot(slot)) return;
            GetSlots(stance)[slot] = null;
            NotifyIfActiveStance(stance);
        }

        public void SetStance(Stance stance)
        {
            if (CurrentStance == stance) return;
            CurrentStance = stance;
            ActiveAbilitiesChanged?.Invoke();
        }

        private List<IAbility> GetLearned(Stance stance)
        {
            if (_learnedByStance.TryGetValue(stance, out var learned)) return learned;

            learned = [];
            _learnedByStance[stance] = learned;
            return learned;
        }

        private IAbility?[] GetSlots(Stance stance)
        {
            if (_slotsByStance.TryGetValue(stance, out var slots)) return slots;

            slots = new IAbility?[slotsPerStance];
            _slotsByStance[stance] = slots;
            return slots;
        }

        private bool IsValidSlot(int slot) => slot >= 0 && slot < slotsPerStance;

        private void EquipToFirstFreeSlot(Stance stance, IAbility ability)
        {
            var slots = GetSlots(stance);
            int freeSlot = Array.IndexOf(slots, null);
            if (freeSlot < 0) return;

            slots[freeSlot] = ability;
            NotifyIfActiveStance(stance);
        }

        private void ClearSlotsOf(Stance stance, IAbility ability)
        {
            var slots = GetSlots(stance);
            for (int i = 0; i < slots.Length; i++)
                if (slots[i]?.IsSame(ability.InstanceId) == true)
                    slots[i] = null;
        }

        private void NotifyIfActiveStance(Stance stance)
        {
            if (stance == CurrentStance) ActiveAbilitiesChanged?.Invoke();
        }
    }
}
