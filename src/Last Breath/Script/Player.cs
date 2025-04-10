namespace Playground
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Localization;
    using Playground.Resource.Quests;
    using Playground.Script;
    using Playground.Script.Abilities;
    using Playground.Script.Abilities.Effects;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Attribute;
    using Playground.Script.BattleSystem;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.Items;
    using Playground.Script.QuestSystem;

    public partial class Player : ObservableCharacterBody2D, ICharacter
    {
        #region Private fields
        private bool _canMove = true, _canFight = true, _isPlayerRunning = false;
        private float _moveProgress = 0f;
        private int _exp, _gold;
        private Stance _stance;
        private AnimatedSprite2D? _sprite;
        private Vector2 _targetPosition, _startPosition;
        private Inventory? _equipInventory, _craftingInventory, _questItemsInventory;
        private List<IAbility> _abilities = [];
        private readonly Dictionary<string, DialogueNode> _dialogs = [];

        private ResourceComponent? _resourceManager;
        private IDamageStrategy? _damageStrategy;
        private DefenseComponent? _playerDefense;
        private EffectsManager? _effectsManager;
        private HealthComponent? _playerHealth;
        private DamageComponent? _playerDamage;
        private readonly ModifierManager _modifierManager = new();
        private readonly AttributeComponent _attribute = new();
        private readonly PlayerProgress _progress = new();
        #endregion

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();

        #region Properties
        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }
        public bool CanFight
        {
            get => _canFight;
            set => _canFight = value;
        }
        public Stance Stance
        {
            get => _stance;
            set
            {
                _stance = value;
                Resource.SetCurrentResource(_stance);
            }
        }

        [Export]
        public bool FirstSpawn { get; set; } = true;
        [Export]
        public int Speed { get; set; } = 200;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
        public PlayerProgress Progress => _progress;
        public DefenseComponent? Defense => _playerDefense;
        public HealthComponent Health => _playerHealth ??= new(_modifierManager);
        public DamageComponent Damage => _playerDamage ??= new(Strategy, _modifierManager);
        public Inventory EquipInventory => _equipInventory ??= new();
        public Inventory CraftingInventory => _craftingInventory ??= new();
        public Inventory QuestItemsInventory => _questItemsInventory ??= new();
        public IDamageStrategy Strategy => _damageStrategy ??= new UnarmedDamageStrategy();
        public EffectsManager Effects => _effectsManager ??= new(this);
        public ModifierManager Modifiers => _modifierManager;
        // i think i need some ability component later, because i need a place where player can modifiy, learn and forget abilities
        public List<IAbility> Abilities => _abilities;

        // TODO: i need default resource
        public ResourceComponent Resource => _resourceManager ??= new(_stance, _modifierManager);
        #endregion

        #region Events
        public event Action<string>? ItemCollected, QuestCompleted, LocationVisited, DialogueCompleted;
        public event Action<EnemyKilledEventArgs>? EnemyKilled;
        #endregion

        public override void _Ready()
        {
            _damageStrategy = new UnarmedDamageStrategy();
            _effectsManager = new(this);
            _playerHealth = new(_modifierManager);
            _playerDamage = new(Strategy, _modifierManager);
            _playerDefense = new(_modifierManager);
            _sprite = GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _sprite.Play("Idle_down");
            _equipInventory = new();
            _craftingInventory = new();
            _questItemsInventory = new();
            LoadDialogues();
            GameManager.Instance!.Player = this;
            _attribute.AddAttribute(new Dexterity(_modifierManager) { InvestedPoints = 5 });
            _attribute.AddAttribute(new Strength(_modifierManager) { InvestedPoints = 5 });
            _modifierManager.AddPermanentModifier(new MaxHealthModifier(ModifierType.Additive, 600));
            _playerHealth.HealUpToMax();
            _abilities.Add(new TouchOfGod(this));
            _abilities.Add(new PrecisionStrike(this));
            GD.Print($"Player health: {_playerHealth.CurrentHealth}");
        }

        public void AddItemToInventory(Item item)
        {
            switch (item.Type)
            {
                case ItemType.Equipment:
                    _equipInventory?.AddItem(item);
                    break;
                case ItemType.Crafting:
                    _craftingInventory?.AddItem(item);
                    break;
                case ItemType.Quest:
                    _questItemsInventory?.AddItem(item);
                    Progress.OnQuestItemCollected(item);
                    break;
                default:
                    break;

            }
            ItemCollected?.Invoke(item.Id);
        }

        public void OnRunAway(Vector2 enemyPosition)
        {
            _targetPosition = enemyPosition;
            _startPosition = Position;
            _moveProgress = 0;
            _isPlayerRunning = true;
        }

        public void OnEquipWeapon(IDamageStrategy strategy) => _playerDamage?.ChangeStrategy(strategy);
        public void OnEnemyKilled(BaseEnemy enemy) => EnemyKilled?.Invoke(new EnemyKilledEventArgs(enemy.EnemyId, enemy.EnemyType));
        public void OnLocationVisited(string id) => LocationVisited?.Invoke(id);


        public void OnDialogueCompleted(string id)
        {
            Progress.OnDialogueCompleted(id);
            DialogueCompleted?.Invoke(id);
        }

        public void OnQuestCompleted(Quest quest)
        {
            Progress?.OnQuestCompleted(quest.Id);
            AcceptReward(quest.GetReward());
            QuestCompleted?.Invoke(quest.Id);
        }

        public void OnTurnEnd()
        {
            Effects.UpdateEffects();
            RecoverResource();
        }

        public void OnFightEnds()
        {
            Effects.RemoveAllEffects();
            // TODO: on reset temporary i still might have some effects in effects manager
            Modifiers.ResetTemporaryModifiers();
        }

        private void RecoverResource()
        {
            // TODO: instead of GoliathEffect should be effect that block resource recovery
            if (Effects.IsEffectApplied(typeof(GoliathEffect)))
            {
                return;
            }
            _resourceManager?.RecoverCurrentResource();
        }

        private void AcceptReward(Reward? reward)
        {
            if (reward == null) return;
            if (reward.Items.Count > 0)
            {
                foreach (var item in reward.Items)
                {
                    AddItemToInventory(item);
                }
            }
            _exp += reward.Exp;
            _gold += reward.Gold;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_isPlayerRunning)
            {
                _moveProgress += (float)delta;
                float t = Mathf.Clamp(_moveProgress / 0.06f, 0f, 6f);
                Position = _targetPosition.Lerp(_startPosition, t);
                if (t >= 6f)
                {
                    _canMove = true;
                    _canFight = true;
                    _isPlayerRunning = false;
                }
            }

            if (!_canMove) return;

            Vector2 inputDirection = Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * Speed;
            MoveAndSlide();
        }

        private void LoadDialogues()
        {
            var dialogueData = ResourceLoader.Load<DialogueData>("res://Resources/Dialogues/PlayerDialogues/playerDialoguesData.tres");
            if (dialogueData.Dialogs == null) return;
            foreach (var item in dialogueData.Dialogs)
            {
                _dialogs.Add(item.Key, item.Value);
            }
        }
    }
}
