namespace Battle.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using Core.Data;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public partial class BattleHud : Control, IHud
    {
        private const string UID = "uid://6d0sr4hy4gg2";
        private IBattleEventBus? _battleEventBus;
        private IUiElementsManager? _uiElementProvider;
        private Dictionary<string, CharacterBar> _characterBars = [];
        private Dictionary<string, QueueSlot> _queueSlots = [];
        private AbilitySlot[] _abilitySlotsInstances = new AbilitySlot[9];
        [Export] private Button? _returnButton;
        [Export] private VBoxContainer? _buttonsContainer;
        [Export] private CharacterBar? _playerBars;
        [Export] private HBoxContainer? _stanceButtons;
        [Export] private GridContainer? _entityBars;
        [Export] private HBoxContainer? _abilitySlots;

        public override void _Ready()
        {
            try
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
            catch (Exception ex)
            {
                GD.Print($"Failed to initialize: {ex.Message}, {ex.StackTrace}");
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
            foreach (var node in _entityBars?.GetChildren() ?? [])
                node.QueueFree();
        }

        public async Task SetupEventBus(IBattleEventBus battleEventBus)
        {
            if (!IsNodeReady()) await ToSignal(this, Node.SignalName.Ready);
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

            _battleEventBus.Subscribe<TurnStartEvent>(OnTurnStart);
            _battleEventBus.Subscribe<PlayerChangesStanceEvent>(OnPlayerChanceStance);

            foreach (AbilitySlot slot in _abilitySlotsInstances)
                slot.SetBattleEventBus(_battleEventBus);
            foreach (StanceSlot stanceSlot in _stanceButtons?.GetChildren().Cast<StanceSlot>() ?? [])
                stanceSlot.SetBattleEventBus(_battleEventBus);
        }

        private void OnPlayerChanceStance(PlayerChangesStanceEvent obj)
        {
            // var player = IPlayer.;
            // if (player == null) return;
            // var skills = player.CurrentStance?.ObtainedAbilities ?? [];
            // for (int i = 0; i < skills.Count; i++)
            //     _abilitySlotsInstances[i].SetAbility(skills[i]);
        }

        public void CreateEntityBarsWithInitialValues(string id, float maxHealth, float maxMana, float currentHealth, float currentMana)
        {
            if (_uiElementProvider == null) return;
            var bar = CharacterBar.Initialize().Instantiate<CharacterBar>();
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
                _uiElementProvider = provider.GetService<IUiElementsManager>();
            }
            catch (Exception ex)
            {
                Tracker.TrackError("Failed to inject services.", ex);
            }
        }

        public void Remove() => GetParent().RemoveChild(this);

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

            if (target is IPlayer) _playerBars?.RemoveEffect(effect);
            else GetCharacterBar(target.InstanceId)?.RemoveEffect(effect);
        }

        private void OnEffectAdded(EffectAddedEvent obj)
        {
            var target = obj.Target;
            var effect = obj.Effect;

            if (target is IPlayer) _playerBars?.AddEffect(effect);
            else GetCharacterBar(target.InstanceId)?.AddEffect(effect);
        }

        private void OnTurnStart(TurnStartEvent obj)
        {
            if (obj.StartedTurn is IPlayer) _buttonsContainer?.Show();
            else _buttonsContainer?.Hide();
        }
    }
}
