namespace Battle.TestData
{
    using Godot;
    using System;
    using Source;
    using Core.Enums;
    using Source.NPC;
    using Core.Modifiers;
    using Core.Interfaces.UI;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Core.Interfaces.Abilities;
    using Source.PassiveSkills;

    [GlobalClass]
    public partial class DevPanel : Control, IInitializable
    {
        private const string UID = "uid://dg7d3dghfdevy";
        private IEntity? _player;
        private ModifierType _modifierType = ModifierType.Flat;
        private EntityParameter _entityParameter = EntityParameter.Damage;
        private DamageType _selectedDamageType = DamageType.Normal;
        private DamageSource _selectedDamageSource = DamageSource.Hit;
        private EntityType _selectedEntityType = EntityType.Regular;
        private Fractions _selectedFraction = Fractions.Undead;
        private Dictionary<int, IModifierInstance> _addedModifiers = new();
        private Dictionary<EntityParameter, Label> _parameterValues = [];
        private Dictionary<string, int> _effectsInstances = [];
        private Dictionary<int, string> _passives = new();
        private long _selectedModifier;
        private string _selectedPassiveSkill;
        private string _passiveToRemove;
        private string _npcCategorySelected = "Necromancer";
        private bool _isCritical;
        [Export] private MainWorld? _mainWorld;
        [Export] private Button? _add, _remove, _flat, _percent, _multiplier, _removeAll;
        [Export] private ItemList? _modifiers;
        [Export] private Button? _removeAllPassives, _manaFull, _mana;
        [Export] private ItemList? _availablePassives, _obtainedPassives;
        [Export] private Button? _heal, _healFull, _dealDamage, _kill;
        [Export] private SpinBox? _healAmount, _damageAmount, _modifierValue, _manaAmount;
        [Export] private OptionButton? _damageType, _damageSource, _parameters;
        [Export] private CheckButton? _critButton;
        [Export] private Button? _addNpc;
        [Export] private OptionButton? _npcType, _npcFraction, _npcCategory;
        [Export] private CheckBox? _isInGroup;
        [Export] private SpinBox? _amountNpc;
        [Export] private VBoxContainer? _statsContainer;
        [Export] private ItemList? _effectsApplied, _effectsManager;
        [Export] private Button? _updateEffects;
        private TaskCompletionSource<Vector2>? _setPoint;

        public override void _Ready()
        {
            _player = Player.Instance;
            _player?.PassiveSkills.SkillAdded += OnPlayerSkillAdded;
            _player?.Parameters.ParameterChanged += OnPlayerParametersChanges;
            _player?.Effects.EffectAdded += OnEffectAdded;
            _player?.Effects.EffectRemoved += OnEffectRemoved;
            _player?.CurrentHealthChanged += (t) =>
            {
                _parameterValues.TryGetValue(EntityParameter.Health, out var label);
                label?.Text = $"{t}";
            };
            _add?.Pressed += OnAddPressed;
            _flat?.Pressed += () => _modifierType = ModifierType.Flat;
            _percent?.Pressed += () => _modifierType = ModifierType.Increase;
            _multiplier?.Pressed += () => _modifierType = ModifierType.Multiplicative;
            _parameters?.ItemSelected += (t) =>
            {
                string text = _parameters?.GetItemText((int)t) ?? string.Empty;
                Enum.TryParse(text, true, out EntityParameter param);
                _entityParameter = param;
            };
            _modifiers?.ItemSelected += (idx) => _selectedModifier = idx;
            _availablePassives?.ItemSelected += OnPassiveSelected;
            _obtainedPassives?.ItemSelected += OnObtainedPassiveSelected;
            _remove?.Pressed += OnRemovePressed;
            _removeAll?.Pressed += OnRemoveAll;
            _healFull?.Pressed += () => _player?.Heal(99999999);
            _kill?.Pressed += () => _player?.Kill();
            _heal?.Pressed += OnHealPressed;
            _dealDamage?.Pressed += OnDamagePressed;
            _addNpc?.Pressed += OnAddNpcPressed;
            _updateEffects?.Pressed += OnUpdatePressed;
            _manaFull?.Pressed += () => { _player?.CurrentMana += 9999; };
            _mana?.Pressed += () =>
            {
                double toRecover = _manaAmount?.Value ?? 0;
                _player?.CurrentMana += (float)toRecover;
            };
            _npcCategory?.ItemSelected += (t) =>
            {
                string text = _npcCategory.GetItemText((int)t);
                _npcCategorySelected = text;
            };
            _npcType?.ItemSelected += (t) =>
            {
                Enum.TryParse(_npcType.GetItemText((int)t), out EntityType type);
                _selectedEntityType = type;
            };
            _damageType?.ItemSelected += (t) =>
            {
                string item = _damageType.GetItemText((int)t);
                Enum.TryParse(item, out DamageType damageType);
                _selectedDamageType = damageType;
            };
            _damageSource?.ItemSelected += (t) =>
            {
                string item = _damageSource.GetItemText((int)t);
                Enum.TryParse(item, out DamageSource damageSource);
                _selectedDamageSource = damageSource;
            };
            _npcFraction?.ItemSelected += (t) =>
            {
                Enum.TryParse(_npcFraction.GetItemText((int)t), out Fractions fraction);
                _selectedFraction = fraction;
            };
            _critButton?.Toggled += (t) => _isCritical = t;
            Visible = false;

            AddTypes<EntityType>(_npcType);
            AddTypes<DamageType>(_damageType);
            AddTypes<DamageSource>(_damageSource);
            AddTypes<EntityParameter>(_parameters);
            AddTypes<Fractions>(_npcFraction);

            AddNpcCategory();

            foreach (var skill in PassiveFactory.Skills)
            {
                string key = skill.Id;
                int idx = _availablePassives.AddItem(key);
                _passives.Add(idx, key);
            }

            foreach (ISkill passiveSkillsSkill in _player?.PassiveSkills.Skills ?? [])
                _obtainedPassives?.AddItem(passiveSkillsSkill.Id);

            foreach (var param in Enum.GetValues<EntityParameter>())
            {
                foreach (IModifierInstance modifierInstance in _player?.Modifiers.GetModifiers(param) ?? [])
                {
                    _modifiers?.AddItem($"{modifierInstance.EntityParameter}, {modifierInstance.ModifierType}, {modifierInstance.Value}");
                }
            }

            CreateLabels();
        }

        private void OnUpdatePressed() => UpdateEffectManager();

        private void OnEffectRemoved(IEffect obj)
        {
            try
            {
                if (_effectsApplied == null) return;
                if (!_effectsInstances.TryGetValue(obj.InstanceId, out int idx))
                    return;

                _effectsApplied.RemoveItem(idx);
                _effectsInstances.Remove(obj.InstanceId);
                UpdateEffectManager();
            }
            catch (Exception ex)
            {
                GD.Print($"{ex.Message}, {ex.StackTrace}");
            }
        }

        private void OnEffectAdded(IEffect obj)
        {
            if (_effectsApplied == null) return;
            int indx = _effectsApplied.AddItem($"{obj.Id}, {obj.Status}, {obj.Duration}");
            _effectsInstances.Add(obj.InstanceId, indx);
            UpdateEffectManager();
        }

        private void UpdateEffectManager()
        {
            var effects = _player?.Effects.Effects;
            if (effects == null || _effectsManager == null) return;
            _effectsManager.Clear();
            foreach (var effect in effects)
                _effectsManager.AddItem($"{effect.Id}, {effect.Status}, {effect.Duration}");
        }

        private void CreateLabels()
        {
            foreach (EntityParameter entityParameter in Enum.GetValues<EntityParameter>())
            {
                var hbox = new HBoxContainer();
                var parameterName = new Label();
                var parameterValue = new Label();
                hbox.AddChild(parameterName);
                hbox.AddChild(parameterValue);
                _statsContainer?.AddChild(hbox);
                parameterName.Text = entityParameter.ToString();
                parameterValue.Text = $"{_player.Parameters.GetValueForParameter(entityParameter):##.##}";
                _parameterValues[entityParameter] = parameterValue;
            }
        }

        private void OnPlayerParametersChanges(EntityParameter parameter, float value)
        {
            _parameterValues.TryGetValue(parameter, out var label);
            label?.Text = value.ToString();
        }

        private void OnPlayerSkillAdded(ISkill obj)
        {
            _obtainedPassives?.AddItem(obj.Id);
        }

        private void AddNpcCategory()
        {
            _npcCategory?.AddItem($"{nameof(Necromancer)}");
            _npcCategory?.AddItem($"{nameof(Bat)}");
        }

        private async void OnAddNpcPressed()
        {
            try
            {
                if (_amountNpc == null) return;
                int amount = (int)_amountNpc.Value;

                var group = new EntityGroup(amount);
                _setPoint = new TaskCompletionSource<Vector2>();

                var spawnPoint = await _setPoint.Task;

                for (int i = 0; i < amount; i++)
                {
                    var npc = NpcFactory.CreateEntity(_npcCategorySelected);
                    if (npc is not INpc s) continue;
                    s.EntityType = _selectedEntityType;
                    s.Fraction = _selectedFraction;
                    if (npc is IEntity entity && _isInGroup.ButtonPressed)
                        group.TryAddToGroup(entity);
                    npc.Position = new Vector2(spawnPoint.X + 50 * i, spawnPoint.Y + 50 * i);
                    _mainWorld?.CallDeferred(Node.MethodName.AddChild, npc);
                }

                _setPoint = null;
            }
            catch (Exception exception)
            {
                GD.Print($"{exception.Message}");
            }
        }

        private void AddTypes<TKey>(OptionButton? button)
            where TKey : struct, Enum
        {
            foreach (var key in Enum.GetValues<TKey>())
                button?.AddItem(key.ToString());
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_setPoint is { Task.IsCompleted: false } && @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                _setPoint.SetResult(GetGlobalMousePosition());

            if (@event is not InputEventKey { CtrlPressed: true, Keycode: Key.Q, Pressed: true })
                return;
            Visible = !Visible;
            AcceptEvent();
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
            _player?.PassiveSkills.RemoveSkill(key);
            _obtainedPassives.RemoveItem((int)index);
        }

        private void OnPassiveSelected(long index)
        {
            var skill = PassiveFactory.GetSkill(_availablePassives?.GetItemText((int)index) ?? string.Empty);
            _player?.PassiveSkills.AddSkill(skill);
        }

        private void OnRemoveAll()
        {
            foreach (KeyValuePair<int, IModifierInstance> modifierInstance in _addedModifiers)
            {
                _modifiers?.RemoveItem(modifierInstance.Key);
                _player?.Modifiers.RemovePermanentModifier(modifierInstance.Value);
            }

            _addedModifiers.Clear();
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
            try
            {
                if (_modifiers == null || _modifierValue == null) return;

                float amount = (float)_modifierValue.Value;
                if (_modifierType is ModifierType.Increase or ModifierType.Multiplicative)
                    amount /= 100;

                var modifier = new ModifierInstance(_entityParameter, _modifierType, amount, this);
                _player?.Modifiers.AddPermanentModifier(modifier);
                int idx = _modifiers.AddItem($"{modifier.EntityParameter}, {modifier.ModifierType}, {modifier.Value}");
                _addedModifiers.Add(idx, modifier);
            }
            catch (Exception ex)
            {
                GD.Print($"{ex.Message}, {ex.StackTrace}");
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
