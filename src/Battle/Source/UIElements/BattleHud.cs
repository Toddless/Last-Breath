namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Constants;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.UI;
    using Godot;

    public partial class BattleHud : Control, IHud
    {
        private const string UID = "uid://6d0sr4hy4gg2";
        private IBattleEventBus? _battleEventBus;
        private IUiElementsManager? _uiElementProvider;
        private Dictionary<string, CharacterBar> _characterBars = [];
        private Dictionary<string, QueueSlot> _queueSlots = [];
        private AbilityButton[] _abilitySlotsInstances = new AbilityButton[BattleConstants.AbilitySlotsPerStance];
        private IAbilityBookComponent? _abilityBook;
        private bool _isPlayerTurn, _isPresenting;
        [Export] private Button? _returnButton;
        [Export] private VBoxContainer? _buttonsContainer;
        [Export] private CharacterBar? _playerBars;
        [Export] private HBoxContainer? _stanceButtons;
        [Export] private VBoxContainer? _entityBars;
        [Export] private HBoxContainer? _abilitySlots;

        public override void _Ready()
        {
            try
            {
                for (int i = 0; i < BattleConstants.AbilitySlotsPerStance; i++)
                {
                    var slot = AbilityButton.Initialize().Instantiate<AbilityButton>();
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
            if (_abilityBook != null) _abilityBook.ActiveAbilitiesChanged -= RefreshAbilitySlots;
            _abilityBook = null;
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
            // Bar values are replay-driven: the BattleDirector republishes these events at the
            // moment the corresponding beat is shown, and the Vitals snapshot carries the numbers.
            _battleEventBus.Subscribe<DamageTakenEvent>(OnDamageTakenReplayed);
            _battleEventBus.Subscribe<EntityHealedEvent>(OnHealedReplayed);
            _battleEventBus.Subscribe<AbilityActivatedEvent>(OnAbilityActivatedReplayed);

            // Max values change rarely (effects/level-ups) and stay live until a restore pipeline exists.
            _battleEventBus.Subscribe<PlayerMaxManaChangesEvent>(OnPlayerMaxManaChanges);
            _battleEventBus.Subscribe<PlayerMaxHealthChanges>(OnPlayerMaxHealthChanges);
            _battleEventBus.Subscribe<EntityMaxHealthChangesEvent>(OnEntityMaxHealthChanges);
            _battleEventBus.Subscribe<EntityMaxManaChangesEvent>(OnEntityMaxManaChanges);
            _battleEventBus.Subscribe<EffectsChangedEvent>(OnEffectsChanged);

            _battleEventBus.Subscribe<TurnStartEvent>(OnTurnStart);
            _battleEventBus.Subscribe<TurnEndEvent>(OnTurnEnd);
            _battleEventBus.Subscribe<PresentationStateChangedEvent>(OnPresentationStateChanged);

            foreach (AbilityButton slot in _abilitySlotsInstances)
                slot.SetBattleEventBus(_battleEventBus);
            foreach (StanceSlot stanceSlot in _stanceButtons?.GetChildren().Cast<StanceSlot>() ?? [])
                stanceSlot.SetBattleEventBus(_battleEventBus);
        }

        public void SetAbilityBook(IAbilityBookComponent abilityBook)
        {
            _abilityBook = abilityBook;
            _abilityBook.ActiveAbilitiesChanged += RefreshAbilitySlots;
            RefreshAbilitySlots();
        }

        /// <summary>Maps the book's slot layout of the current stance 1:1 onto the HUD buttons.</summary>
        private void RefreshAbilitySlots()
        {
            var slots = _abilityBook?.ActiveSlots;
            for (int i = 0; i < _abilitySlotsInstances.Length; i++)
            {
                var ability = slots != null && i < slots.Count ? slots[i] : null;
                if (ability != null) _abilitySlotsInstances[i].SetAbility(ability);
                else _abilitySlotsInstances[i].ClearAbility();
            }
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
            _uiElementProvider = provider.GetService<IUiElementsManager>();
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

        private void OnDamageTakenReplayed(DamageTakenEvent evnt) => UpdateVitals(evnt.Target, evnt.Vitals);

        private void OnHealedReplayed(EntityHealedEvent evnt) => UpdateVitals(evnt.Healed, evnt.Vitals);

        private void OnAbilityActivatedReplayed(AbilityActivatedEvent evnt) => UpdateVitals(evnt.Caster, evnt.Vitals);

        /// <summary>Bars always show the snapshot, never live state — live state is "from the future" during replay.</summary>
        private void UpdateVitals(IFightable entity, VitalsSnapshot vitals)
        {
            var bar = entity is IPlayer ? _playerBars : GetCharacterBar(entity.InstanceId);
            if (bar == null) return;

            bar.UpdateHealth(vitals.Health);
            bar.UpdateMana(vitals.Mana);
        }

        private void OnEntityMaxManaChanges(EntityMaxManaChangesEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateMaxMana(obj.Value);
        }

        private void OnEntityMaxHealthChanges(EntityMaxHealthChangesEvent obj)
        {
            GetCharacterBar(obj.Entity.InstanceId)?.UpdateMaxHealth(obj.Value);
        }

        private void OnEffectsChanged(EffectsChangedEvent obj)
        {
            var bar = obj.Target is IPlayer ? _playerBars : GetCharacterBar(obj.Target.InstanceId);
            bar?.SetEffects(obj.Effects);
        }

        private void OnTurnStart(TurnStartEvent obj)
        {
            _isPlayerTurn = obj.StartedTurn is IPlayer;
            if (_isPlayerTurn) _buttonsContainer?.Show();
            else _buttonsContainer?.Hide();

            ApplyInputWindow();
        }

        private void OnTurnEnd(TurnEndEvent obj)
        {
            _isPlayerTurn = false;
            ApplyInputWindow();
        }

        private void OnPresentationStateChanged(PresentationStateChangedEvent evnt)
        {
            _isPresenting = evnt.IsPlaying;
            ApplyInputWindow();
        }

        /// <summary>
        /// The player input window: clicks and hotkeys work only during the player's turn
        /// while nothing is being animated. Slots stay visible (tooltips keep working),
        /// only activation input is blocked. A stunned player never gets an open window:
        /// his TurnStart and TurnEnd resolve within one synchronous logic run.
        /// </summary>
        private void ApplyInputWindow()
        {
            bool open = _isPlayerTurn && !_isPresenting;
            foreach (AbilityButton slot in _abilitySlotsInstances)
                slot.SetInputEnabled(open);
            foreach (StanceSlot stanceSlot in _stanceButtons?.GetChildren().Cast<StanceSlot>() ?? [])
                stanceSlot.Disabled = !open;
        }
    }
}
