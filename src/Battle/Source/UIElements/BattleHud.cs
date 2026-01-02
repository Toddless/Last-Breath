namespace Battle.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
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
        private IUiElementProvider? _uiElementProvider;
        private Dictionary<string, CharacterBar> _characterBars = [];
        private Dictionary<string, QueueSlot> _queueSlots = [];
        private AbilitySlot[] _abilitySlotsInstances = new AbilitySlot[9];
        [Export] private Button? _returnButton;
        [Export] private VBoxContainer? _buttonsContainer;
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
                slot.SetNumber(i + 1);
                _abilitySlots?.AddChild(slot);
                _abilitySlotsInstances[i] = slot;
            }

            var buttonGroup = new ButtonGroup { AllowUnpress = false };

            for (int i = 0; i < 3; i++)
            {
                var slot = StanceSlot.Initialize().Instantiate<StanceSlot>();
                slot.SetStance((Stance)i);
                slot.ButtonGroup = buttonGroup;
                _stanceButtons?.AddChild(slot);
            }
        }

        public override void _ExitTree()
        {
            _battleEventBus = null;
            _characterBars.Clear();
            _queueSlots.Clear();
            _playerBars?.ClearEffects();
            foreach (StanceSlot stanceSlot in _stanceButtons?.GetChildren().Cast<StanceSlot>() ?? [])
                stanceSlot.RemoveBattleEventBus();
            foreach (Node child in _queue?.GetChildren() ?? [])
                child.QueueFree();
            foreach (var node in _entityBars?.GetChildren() ?? [])
                node.QueueFree();
        }

        public void SetupEventBus(IBattleEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<PlayerManaChangesEvent>(OnPlayerManaChanges);
            _battleEventBus.Subscribe<PlayerMaxManaChangesEvent>(OnPlayerMaxManaChanges);
            _battleEventBus.Subscribe<PlayerHealthChangesEvent>(OnPlayerHealthChanges);
            _battleEventBus.Subscribe<PlayerMaxHealthChanges>(OnPlayerMaxHealthChanges);

            _battleEventBus.Subscribe<EntityHealthChangesEvent>(OnEntityHealthChanges);
            _battleEventBus.Subscribe<EntityMaxHealthChangesEvent>(OnEntityMaxHealthChanges);
            _battleEventBus.Subscribe<EntityManaChangesEvent>(OnEntityManaChanges);
            _battleEventBus.Subscribe<EntityMaxManaChangesEvent>(OnEntityMaxManaChanges);

            _battleEventBus.Subscribe<EffectAddedEvent>(OnEffectAdded);
            _battleEventBus.Subscribe<EffectRemovedEvent>(OnEffectRemoved);

            _battleEventBus.Subscribe<BattleQueueDefinedEvent>(OnQueueDefined);
            _battleEventBus.Subscribe<TurnStartEvent>(OnTurnStart);
            _battleEventBus.Subscribe<TurnEndEvent>(OnTurnEnd);
            _battleEventBus.Subscribe<PlayerChangesStanceEvent>(OnPlayerChanceStance);

            foreach (AbilitySlot slot in _abilitySlotsInstances)
                slot.SetBattleEventBus(_battleEventBus);
            foreach (StanceSlot stanceSlot in _stanceButtons?.GetChildren().Cast<StanceSlot>() ?? [])
                stanceSlot.SetBattleEventBus(_battleEventBus);
        }

        private void OnPlayerChanceStance(PlayerChangesStanceEvent obj)
        {
            var player = Player.Instance;
            if (player == null) return;
            var skills = player.CurrentStance?.ObtainedAbilities ?? [];
            for (int i = 0; i < skills.Count; i++)
                _abilitySlotsInstances[i].SetAbility(skills[i]);
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
                _uiElementProvider = provider.GetService<IUiElementProvider>();
            }
            catch (Exception ex)
            {
                Tracker.TrackError("Failed to inject services.", ex);
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private CharacterBar? GetCharacterBar(string id) => _characterBars.GetValueOrDefault(id);

        private void OnPlayerMaxManaChanges(PlayerMaxManaChangesEvent obj)
        {
            _playerBars?.UpdateMaxMana(obj.Value);
        }

        private void OnPlayerMaxHealthChanges(PlayerMaxHealthChanges obj)
        {
            _playerBars?.UpdateMaxHealth(obj.Value);
        }

        private void OnPlayerHealthChanges(PlayerHealthChangesEvent obj)
        {
            _playerBars?.UpdateHealth(obj.Value);
        }

        private void OnPlayerManaChanges(PlayerManaChangesEvent obj)
        {
            _playerBars?.UpdateMana(obj.Value);
        }

        private void OnEntityManaChanges(EntityManaChangesEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateMana(obj.Value);
        }

        private void OnEntityHealthChanges(EntityHealthChangesEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateHealth(obj.Value);
        }

        private void OnEntityMaxManaChanges(EntityMaxManaChangesEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateMaxMana(obj.Value);
        }

        private void OnEntityMaxHealthChanges(EntityMaxHealthChangesEvent obj)
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

        private void OnTurnEnd(TurnEndEvent obj)
        {
            var entity = obj.CompletedTurn;
            _queueSlots.TryGetValue(entity.InstanceId, out QueueSlot? slot);
            if (slot != null) _queue?.CallDeferred(Node.MethodName.RemoveChild, slot);
        }

        private void OnTurnStart(TurnStartEvent obj)
        {
            if (obj.StartedTurn is Player) _buttonsContainer?.Show();
            else _buttonsContainer?.Hide();
        }

        private void OnQueueDefined(BattleQueueDefinedEvent obj)
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
