namespace Battle.TestData
{
    using Godot;
    using System;
    using Services;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Interfaces.Skills;
    using Source.Abilities.PassiveSkills;

    [GlobalClass]
    [Tool]
    public partial class DevPanel : Control
    {
        private IEntity? _player;
        private ModifierType _modifierType = ModifierType.Flat;
        private EntityParameter _entityParameter = EntityParameter.Armor;
        private DamageType _selectedDamageType = DamageType.Normal;
        private DamageSource _selectedDamageSource = DamageSource.Hit;
        private Dictionary<int, IModifierInstance> _addedModifiers = new();
        private Dictionary<int, string> _passives = new();
        private long _selectedModifier;
        private string _selectedPassiveSkill;
        private string _passiveToRemove;
        private bool _isCritical;
        [Export] private Button? _add, _remove, _flat, _percent, _multiplier, _removeAll;
        [Export] private ItemList? _modifiers;
        [Export] private LineEdit? _amount;
        [Export] private Button? _addPassive, _removePassive, _removeAllPassives;
        [Export] private ItemList? _availablePassives, _obtainedPassives;
        [Export] private Button? _heal, _healFull, _dealDamage, _kill;
        [Export] private SpinBox? _healAmount, _damageAmount;
        [Export] private OptionButton? _damageType, _damageSource, _parameters;
        [Export] private CheckButton? _critButton;


        public override void _Ready()
        {
            _player = GameServiceProvider.Instance.GetService<PlayerReference>().GetPlayer();
            _player.PassiveSkills.SkillAdded += OnSkillAdded;
            _add?.Pressed += OnAddPressed;
            _flat?.Pressed += () => _modifierType = ModifierType.Flat;
            _percent?.Pressed += () => _modifierType = ModifierType.Increase;
            _multiplier?.Pressed += () => _modifierType = ModifierType.Multiplicative;
            _parameters?.ItemSelected += OnParameterSelected;
            _modifiers?.ItemSelected += (idx) => _selectedModifier = idx;
            _availablePassives?.ItemSelected += OnPassiveSelected;
            _obtainedPassives?.ItemSelected += OnObtainedPassiveSelected;
            _remove?.Pressed += OnRemovePressed;
            _removeAll?.Pressed += OnRemoveAll;
            _addPassive?.Pressed += OnAddPassivePressed;
            _healFull?.Pressed += () => _player.Heal(99999999);
            _kill?.Pressed += () => _player.Kill();
            _heal?.Pressed += OnHealPressed;
            _dealDamage?.Pressed += OnDamagePressed;
            _damageType?.ItemSelected += OnDamageTypeSelected;
            _damageSource?.ItemSelected += OnDamageSourceSelected;
            _critButton?.Toggled += (t) => _isCritical = t;
            Visible = false;
            SetParameters();
            SetPassiveSkills();
            SetDamageTypes();
            SetDamageSource();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventKey { CtrlPressed: true, Keycode: Key.Q, Pressed: true })
                return;
            Visible = !Visible;
            AcceptEvent();
        }

        private void SetDamageSource()
        {
            foreach (DamageSource damageSource in Enum.GetValues<DamageSource>())
                _damageSource?.AddItem(damageSource.ToString());
        }

        private void SetDamageTypes()
        {
            foreach (DamageType damageType in Enum.GetValues<DamageType>())
                _damageType?.AddItem(damageType.ToString());
        }

        private void SetPassiveSkills()
        {
            if (_availablePassives == null) return;

            foreach (var skill in PassiveFactory.Skills)
            {
                string key = skill.Id;
                int idx = _availablePassives.AddItem(key);
                _passives.Add(idx, key);
            }
        }

        private void SetParameters()
        {
            foreach (EntityParameter entityParameter in Enum.GetValues<EntityParameter>())
                _parameters?.AddItem(entityParameter.ToString());
        }

        private void OnDamageSourceSelected(long index)
        {
            if (_damageSource == null) return;
            string item = _damageSource.GetItemText((int)index);
            Enum.TryParse(item, out DamageSource damageSource);
            _selectedDamageSource = damageSource;
        }

        private void OnDamageTypeSelected(long index)
        {
            if (_damageType == null) return;
            string item = _damageType.GetItemText((int)index);
            Enum.TryParse(item, out DamageType damageType);
            _selectedDamageType = damageType;
        }

        private void OnDamagePressed()
        {
            if (_damageAmount == null) return;
            _player?.TakeDamage(_player, (float)_damageAmount.Value, _selectedDamageType, _selectedDamageSource, _isCritical);
        }

        private void OnHealPressed()
        {
            if (_healAmount == null) return;
            double amount = _healAmount.Value;
            _player?.Heal((float)amount);
        }

        private void OnObtainedPassiveSelected(long index)
        {
            if (_obtainedPassives == null) return;
            string key = _obtainedPassives.GetItemText((int)index);
            var skill = PassiveFactory.GetSkill(key);
            _player?.PassiveSkills.RemoveSkill(skill.Id);
            _passiveToRemove = string.Empty;
        }

        private void OnAddPassivePressed()
        {
            var skill = PassiveFactory.GetSkill(_selectedPassiveSkill);
            _player?.PassiveSkills.AddSkill(skill);
        }

        private void OnPassiveSelected(long index)
        {
            _selectedPassiveSkill = _availablePassives?.GetItemText((int)index) ?? string.Empty;
        }

        private void OnSkillAdded(ISkill obj)
        {
            _obtainedPassives?.AddItem(obj.Id);
        }




        private void OnRemoveAll()
        {
            foreach (KeyValuePair<int, IModifierInstance> modifierInstance in _addedModifiers)
            {
                _modifiers?.RemoveItem(modifierInstance.Key);
                _player?.Modifiers.RemovePermanentModifier(modifierInstance.Value);
            }
        }

        private void OnRemovePressed()
        {
            if (!_addedModifiers.TryGetValue((int)_selectedModifier, out var modifier)) return;

            _modifiers?.RemoveItem((int)_selectedModifier);
            _player?.Modifiers.RemovePermanentModifier(modifier);
            _addedModifiers.Remove((int)_selectedModifier);
        }


        private void OnAddPressed()
        {
            if (_modifiers == null) return;
            if (!int.TryParse(_amount?.Text, out int amount))
                return;

            var modifier = new ModifierInstance(_entityParameter, _modifierType, amount, this);
            _player?.Modifiers.AddPermanentModifier(modifier);
            int idx = _modifiers.AddItem($"{modifier.EntityParameter}, {modifier.ModifierType}, {modifier.Value}");
            _addedModifiers.Add(idx, modifier);
        }


        private void OnParameterSelected(long index)
        {
            string text = _parameters?.GetItemText((int)index) ?? string.Empty;
            Enum.TryParse(text, true, out EntityParameter param);
            _entityParameter = param;
        }


    }
}
