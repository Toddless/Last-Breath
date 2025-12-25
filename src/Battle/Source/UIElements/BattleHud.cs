namespace Battle.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using System.Linq;
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
        [Export] private HBoxContainer? _abilitySlots;


        public override void _Ready()
        {
            for (int i = 0; i < 9; i++)
            {
                var slot = AbilitySlot.Initialize().Instantiate<AbilitySlot>();
                _abilitySlots?.AddChild(slot);
            }
        }

        public override void _ExitTree()
        {
            _battleEventBus = null;
            _characterBars.Clear();
            _queueSlots.Clear();
            _playerBars?.ClearEffects();
            foreach (Node child in _queue?.GetChildren() ?? [])
                child.QueueFree();
            foreach (var node in _entityBars?.GetChildren() ?? [])
                node.QueueFree();
        }

        public void SetupEventBus(IBattleEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<PlayerManaChangesGameEvent>(OnPlayerManaChanges);
            _battleEventBus.Subscribe<PlayerMaxManaChangesGameEvent>(OnPlayerMaxManaChanges);
            _battleEventBus.Subscribe<PlayerHealthChangesGameEvent>(OnPlayerHealthChanges);
            _battleEventBus.Subscribe<PlayerMaxHealthChanges>(OnPlayerMaxHealthChanges);

            _battleEventBus.Subscribe<EntityHealthChangesGameEvent>(OnEntityHealthChanges);
            _battleEventBus.Subscribe<EntityMaxHealthChangesGameEvent>(OnEntityMaxHealthChanges);
            _battleEventBus.Subscribe<EntityManaChangesGameEvent>(OnEntityManaChanges);
            _battleEventBus.Subscribe<EntityMaxManaChangesGameEvent>(OnEntityMaxManaChanges);

            _battleEventBus.Subscribe<EffectAddedEvent>(OnEffectAdded);
            _battleEventBus.Subscribe<EffectRemovedEvent>(OnEffectRemoved);

            _battleEventBus.Subscribe<BattleQueueDefinedGameEvent>(OnQueueDefined);
            _battleEventBus.Subscribe<TurnStartGameEvent>(OnTurnStart);
            _battleEventBus.Subscribe<TurnEndGameEvent>(OnTurnEnd);
            foreach (AbilitySlot slot in _abilitySlots?.GetChildren().Cast<AbilitySlot>() ?? [])
                slot.SetBattleEventBus(_battleEventBus);
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

        private CharacterBar? GetCharacterBar(string id) => _characterBars.GetValueOrDefault(id);

        private void OnPlayerMaxManaChanges(PlayerMaxManaChangesGameEvent obj)
        {
            _playerBars?.UpdateMaxMana(obj.Value);
        }

        private void OnPlayerMaxHealthChanges(PlayerMaxHealthChanges obj)
        {
            _playerBars?.UpdateMaxHealth(obj.Value);
        }

        private void OnPlayerHealthChanges(PlayerHealthChangesGameEvent obj)
        {
            _playerBars?.UpdateHealth(obj.Value);
        }

        private void OnPlayerManaChanges(PlayerManaChangesGameEvent obj)
        {
            _playerBars?.UpdateMana(obj.Value);
        }

        private void OnEntityManaChanges(EntityManaChangesGameEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateMana(obj.Value);
        }

        private void OnEntityHealthChanges(EntityHealthChangesGameEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateHealth(obj.Value);
        }

        private void OnEntityMaxManaChanges(EntityMaxManaChangesGameEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateMaxMana(obj.Value);
        }

        private void OnEntityMaxHealthChanges(EntityMaxHealthChangesGameEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateMaxHealth(obj.Value);
        }

        private void OnEffectRemoved(EffectRemovedEvent obj)
        {
            var target = obj.Target;
            var effect = obj.Effect;

            if (target is Player) _playerBars?.RemoveEffect(effect);
            else GetCharacterBar(target.InstanceId)?.RemoveEffect(effect);
        }

        private void OnEffectAdded(EffectAddedEvent obj)
        {
            var target = obj.Target;
            var effect = obj.Effect;

            if (target is Player) _playerBars?.AddEffect(effect);
            else GetCharacterBar(target.InstanceId)?.AddEffect(effect);
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
