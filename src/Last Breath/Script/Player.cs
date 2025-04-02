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
    using Playground.Script.BattleSystem;
    using Playground.Script.Helpers;
    using Playground.Script.Items;
    using Playground.Script.QuestSystem;

    public partial class Player : ObservableCharacterBody2D
    {
        #region Private fields
        private AnimatedSprite2D? _sprite;
        private Vector2 _lastPosition;
        private bool _canMove = true;
        private readonly PlayerProgress _progress = new();
        private readonly Dictionary<string, DialogueNode> _dialogs = [];
        private Inventory? _equipInventory, _craftingInventory, _questItemsInventory;
        private int _exp;
        private int _gold;
        #endregion

        #region Components
        private HealthComponent? _playerHealth;
        private DamageComponent? _playerDamage;
        private readonly ModifierManager _modifierManager = new();
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
        public int Speed { get; set; } = 200;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
        public HealthComponent? PlayerHealth
        {
            get => _playerHealth;
            set => _playerHealth = value;
        }
        public DamageComponent? PlayerDamage
        {
            get => _playerDamage;
            set => _playerDamage = value;
        }
        public PlayerProgress Progress => _progress;

        public Inventory EquipInventory => _equipInventory ??= new();
        public Inventory CraftingInventory => _craftingInventory ??= new();
        public Inventory QuestItemsInventory => _questItemsInventory ??= new();
        #endregion

        #region Events
        public event Action<string>? ItemCollected, QuestCompleted, LocationVisited, DialogueCompleted;
        public event Action<EnemyKilledEventArgs>? EnemyKilled;
        #endregion

        public override void _Ready()
        {
            _playerHealth = new(_modifierManager);
            _playerDamage = new(new UnarmedDamageStrategy(), _modifierManager);
            _sprite = GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _sprite.Play("Idle_down");
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

        public void OnEquipWeapon(IDamageStrategy strategy) => _playerDamage?.ChangeStrategy(strategy);

        public void OnEnemyKilled(BaseEnemy enemy) => EnemyKilled?.Invoke(new EnemyKilledEventArgs(enemy.EnemyId, enemy.EnemyType));

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
        public void OnLocationVisited(string id) => LocationVisited?.Invoke(id);

        private void AcceptReward(Reward? reward)
        {
            if(reward == null) return;
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
