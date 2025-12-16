namespace Battle
{
    using Godot;
    using System;
    using Utilities;
    using Source.UIElements;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public partial class BattleHud : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://2w3t3maumkh6";
        private IBattleEventBus? _battleEventBus;
        private IUIElementProvider? _uiElementProvider;
        private Dictionary<string, CharacterBar> _characterBars = [];
        private Dictionary<string, QueueSlot> _queueSlots = [];
        [Export] private Button? _returnButton;
        [Export] private CharacterBar? _playerBars;
        [Export] private HBoxContainer? _stanceButtons, _abilityButtons;
        [Export] private GridContainer? _entityBars;
        [Export] private HBoxContainer? _queue;

        public override void _ExitTree()
        {
            _battleEventBus?.Unsubscribe<PlayerHealthChangesGameEvent>(OnPlayerHealthChanges);
            _battleEventBus?.Unsubscribe<PlayerManaChangesGameEvent>(OnPlayerManaChanges);
            _battleEventBus?.Unsubscribe<EntityHealthChanges>(OnEntityHealthChanges);
            _battleEventBus?.Unsubscribe<EntityManaChanges>(OnEntityManaChanges);
            _battleEventBus?.Unsubscribe<BattleQueueDefinedGameEvent>(OnQueueDefined);
            _battleEventBus?.Unsubscribe<TurnStartGameEvent>(OnTurnStart);
            _battleEventBus?.Unsubscribe<TurnEndGameEvent>(OnTurnEnd);
            _battleEventBus = null;
            _characterBars.Clear();
            _queueSlots.Clear();
            foreach (Node child in _queue?.GetChildren() ?? [])
                child.QueueFree();
            foreach (var node in _entityBars?.GetChildren() ?? [])
                node.QueueFree();
        }

        public void SetupEventBus(IBattleEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<PlayerHealthChangesGameEvent>(OnPlayerHealthChanges);
            _battleEventBus.Subscribe<PlayerManaChangesGameEvent>(OnPlayerManaChanges);
            _battleEventBus.Subscribe<EntityHealthChanges>(OnEntityHealthChanges);
            _battleEventBus.Subscribe<EntityManaChanges>(OnEntityManaChanges);
            _battleEventBus.Subscribe<BattleQueueDefinedGameEvent>(OnQueueDefined);
            _battleEventBus.Subscribe<TurnStartGameEvent>(OnTurnStart);
            _battleEventBus.Subscribe<TurnEndGameEvent>(OnTurnEnd);
        }

        private void OnTurnEnd(TurnEndGameEvent obj)
        {
            var entity = obj.CompletedTurn;
            _queueSlots.TryGetValue(entity.InstanceId, out QueueSlot? slot);
            if (slot != null) _queue?.CallDeferred(Node.MethodName.RemoveChild, slot);
        }

        private void OnTurnStart(TurnStartGameEvent obj)
        {
        }


        public void CreateEntityBarsWithInitialValues(string id, float maxHealth, float maxMana, float currentHealth, float currentMana)
        {
            if (_uiElementProvider == null) return;
            var bar = _uiElementProvider.Create<CharacterBar>();
            bar.SetInitialValues(maxMana, currentMana, maxHealth, currentHealth);
            bar.FlipH = true;
            _characterBars.Add(id, bar);
            _entityBars?.AddChild(bar);
        }

        public void SetPlayerInitialValues(float maxHealth, float maxMana, float health, float mana)
        {
            _playerBars?.SetInitialValues(maxMana, mana, maxHealth, health);
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            try
            {
                _uiElementProvider = provider.GetService<IUIElementProvider>();
            }
            catch (Exception ex)
            {
                Tracker.TrackError("Failed to inject services.", ex);
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);


        private void OnPlayerHealthChanges(PlayerHealthChangesGameEvent obj)
        {
            _playerBars?.UpdateHealth(obj.Value);
        }

        private void OnPlayerManaChanges(PlayerManaChangesGameEvent obj)
        {
            _playerBars?.UpdateMana(obj.Value);
        }

        private void OnEntityManaChanges(EntityManaChanges obj)
        {
            string id = obj.Entity.InstanceId;
            float value = obj.Value;
            _characterBars.TryGetValue(id, out CharacterBar? bar);
            bar?.UpdateMana(value);
        }

        private void OnEntityHealthChanges(EntityHealthChanges obj)
        {
            string id = obj.Entity.InstanceId;
            float value = obj.Value;
            _characterBars.TryGetValue(id, out CharacterBar? bar);
            bar?.UpdateHealth(value);
        }

        private void OnQueueDefined(BattleQueueDefinedGameEvent obj)
        {
            if (_uiElementProvider == null) return;
            var queue = obj.Entities;
            foreach (var entity in queue)
            {
                if (!_queueSlots.TryGetValue(entity.InstanceId, out QueueSlot? slot))
                {
                    slot = _uiElementProvider.Create<QueueSlot>();
                    if (entity.Icon != null) slot.SetIcon(entity.Icon);
                    _queueSlots.Add(entity.InstanceId, slot);
                }

                _queue?.CallDeferred(Node.MethodName.AddChild, slot);
            }
        }
    }
}
