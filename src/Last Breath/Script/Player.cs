namespace Playground
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Godot;
    using Playground.Components;
    using Playground.Localization;
    using Playground.Script;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Helpers;
    using Playground.Script.Items;
    using Playground.Script.QuestSystem;
    using Playground.Script.Reputation;

    public partial class Player : ObservableCharacterBody2D, ICharacter
    {
        #region Private fields
        private AnimatedSprite2D? _sprite;
        private Vector2 _lastPosition;
        private bool _canMove = true;
        private ObservableCollection<IEffect>? _effects;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private PlayerProgress _progress = new();
        private Dictionary<string, DialogueNode> _dialogs = [];
        private List<IAbility>? _abilities;
        private Sprite2D? _playerAvatar;
        private Inventory? _equipInventory, _craftingInventory, _questItemsInventory;
        private int _exp;
        private int _gold;
        #endregion

        #region Components
        private EffectManager? _effectManager;
        private HealthComponent? _playerHealth;
        private AttackComponent? _playerAttack;
        private ReputationManager? _reputation;
        #endregion

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();

        #region Properties
        public Vector2 PlayerLastPosition
        {
            get => _lastPosition;
            set => _lastPosition = value;
        }
        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }

        [Export]
        public bool FirstSpawn { get; set; } = true;
        [Export]
        [Changeable]
        public int Speed { get; set; } = 200;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
        [Changeable]
        public HealthComponent? HealthComponent
        {
            get => _playerHealth;
            set => _playerHealth = value;
        }
        [Changeable]
        public AttackComponent? AttackComponent
        {
            get => _playerAttack;
            set => _playerAttack = value;
        }
        [Changeable]
        public ReputationManager? Reputation => _reputation;
        public PlayerProgress Progress => _progress;
        public EffectManager? EffectManager => _effectManager;
        public ObservableCollection<IAbility>? AppliedAbilities { get => _appliedAbilities; set => _appliedAbilities = value; }
        public List<IAbility>? Abilities => _abilities;
        public Sprite2D? PlayerAvatar => _playerAvatar;

        public Inventory EquipInventory => _equipInventory ??= new();
        public Inventory CraftingInventory => _craftingInventory ??= new();
        public Inventory QuestItemsInventory => _questItemsInventory ??= new();
        #endregion

        #region Events
        public event Action<string>? ItemCollected, EnemyKilled, QuestCompleted, LocationVisited, DialogueCompleted;
        #endregion

        public override void _Ready()
        {
            _effects = [];
            _effectManager = new(_effects);
            _playerHealth = new(_effectManager.CalculateValues);
            _playerAttack = new(_effectManager.CalculateValues);
            _sprite = GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _playerAvatar = GetNode<Sprite2D>(nameof(Sprite2D));
            _sprite.Play("Idle_down");
            _reputation = new(0, 0, 0);
            _equipInventory = new();
            _craftingInventory = new();
            _questItemsInventory = new();
            LoadDialogues();
            GameManager.Instance!.Player = this;
        }

        public void AddItemToInventory(Item item)
        {
            switch (item.Type)
            {
                case Script.Enums.ItemType.Equipment:
                    _equipInventory?.AddItem(item);
                    break;
                case Script.Enums.ItemType.Crafting:
                    _craftingInventory?.AddItem(item);
                    break;
                case Script.Enums.ItemType.Quest:
                    _questItemsInventory?.AddItem(item);
                    Progress.OnQuestItemCollected(item);
                    break;
                default:
                    break;

            }
            ItemCollected?.Invoke(item.Id);
        }

        public void OnDialogueCompleted(string id)
        {
            Progress.OnDialogueCompleted(id);
            DialogueCompleted?.Invoke(id);
        }

        public void OnQuestCompleted(string id)
        {
            Progress?.OnQuestCompleted(id);
            QuestCompleted?.Invoke(id);
        }

        public void AcceptReward(Reward reward)
        {
            if (reward.Items.Count > 0)
            {
                foreach (var item in reward.Items)
                {
                    AddItemToInventory(item);
                }
            }
            _exp += reward.Exp;
            _gold += reward.Gold;
            reward.Free();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!_canMove)
            {
                return;
            }

            Vector2 inputDirection = Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * Speed;
            MoveAndSlide();
        }

        private void LoadDialogues()
        {
            var dialogueData = ResourceLoader.Load<DialogueData>("res://Resource/Dialogues/PlayerDialogues/playerDialoguesData.tres");
            if (dialogueData.Dialogs == null) return;
            foreach (var item in dialogueData.Dialogs)
            {
                _dialogs.Add(item.Key, item.Value);
            }
        }
    }
}
