namespace Battle.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Constants;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Views.UI;
    using Godot;

    public partial class BattleHud : Control, IHud
    {
        private const string UID = "uid://6d0sr4hy4gg2";
        private static readonly Color s_queueCurrentColor = new(1f, 1f, 1f);
        private static readonly Color s_queueWaitingColor = new(1f, 1f, 1f, 0.45f);
        private IBattleEventBus? _battleEventBus;
        private IUiElementsManager? _uiElementProvider;
        private Dictionary<string, CharacterBar> _characterBars = [];
        private readonly Dictionary<string, Label> _queueLabels = [];
        private AbilityButton[] _abilitySlotsInstances = new AbilityButton[BattleConstants.AbilitySlotsPerStance];
        private static readonly float[] s_playbackSpeeds = [1f, 2f, 3f];
        private IAbilityBookComponent? _abilityBook;
        private Button? _endTurnButton, _fleeButton, _speedButton;
        private int _speedIndex;
        private bool _isPlayerTurn, _isPresenting, _isSelectingTargets;
        [Export] private VBoxContainer? _buttonsContainer;
        [Export] private CharacterBar? _playerBars;
        [Export] private HBoxContainer? _stanceButtons;
        [Export] private BattleLog? _battleLog;
        [Export] private VBoxContainer? _entityBars;
        [Export] private HBoxContainer? _abilitySlots;
        [Export] private HBoxContainer? _queueContainer;

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
                    HoverTooltip.Attach(slot, () => ShowAbilityTooltip(slot));
                }

                var buttonGroup = new ButtonGroup { AllowUnpress = false };

                for (int i = 0; i < 3; i++)
                {
                    var slot = StanceSlot.Initialize().Instantiate<StanceSlot>();
                    slot.SetStance((Stance)i);
                    slot.ButtonGroup = buttonGroup;
                    _stanceButtons?.AddChild(slot);
                    HoverTooltip.Attach(slot, () => ShowStanceTooltip(slot.Stance));
                }

                CreateTurnButtons();
            }
            catch (Exception ex)
            {
                GD.Print($"Failed to initialize: {ex.Message}, {ex.StackTrace}");
            }
        }

        /// <summary>Grey-UI placeholders built in code into the existing turn-buttons container
        /// (it already shows only on the player's turn).</summary>
        private void CreateTurnButtons()
        {
            _endTurnButton = new Button { Text = Core.Localization.Localization.Localize("UI_End_Turn") };
            _endTurnButton.Pressed += () => _battleEventBus?.Publish(new PlayerEndTurnRequestedEvent());
            _endTurnButton.FocusMode = FocusModeEnum.None;
            _buttonsContainer?.AddChild(_endTurnButton);

            _fleeButton = new Button { Text = Core.Localization.Localization.Localize("UI_Flee") };
            _fleeButton.Pressed += () => _battleEventBus?.Publish(new PlayerFleeAttemptEvent());
            _fleeButton.FocusMode = FocusModeEnum.None;
            _buttonsContainer?.AddChild(_fleeButton);

            _speedButton = new Button { Text = SpeedLabel(s_playbackSpeeds[_speedIndex]) };
            _speedButton.Pressed += CyclePlaybackSpeed;
            _speedButton.FocusMode = FocusModeEnum.None;
            _buttonsContainer?.AddChild(_speedButton);
        }

        private void CyclePlaybackSpeed()
        {
            _speedIndex = (_speedIndex + 1) % s_playbackSpeeds.Length;
            float speed = s_playbackSpeeds[_speedIndex];
            if (_speedButton != null) _speedButton.Text = SpeedLabel(speed);
            _battleEventBus?.Publish(new PlaybackSpeedChangedEvent(speed));
        }

        private static string SpeedLabel(float speed) => $"×{speed:0}";

        private IPopup? ShowAbilityTooltip(AbilityButton slot)
        {
            if (slot.CurrentAbility is not { } ability) return null;
            var popup = _uiElementProvider?.ShowPopup(typeof(TextTooltipPopup)) as TextTooltipPopup;
            popup?.Show(ability.DisplayName, AbilityInfoLine(ability), ability.Description);
            return popup;
        }

        private static string AbilityInfoLine(Core.Battle.Abilities.IAbility ability)
        {
            string cost = $"{ability.CostValue} {ability.CostType}";
            return ability.Cooldown > 0
                ? $"{cost} · {Core.Localization.Localization.Localize("UI_Cooldown")} {ability.Cooldown:0.#}"
                : cost;
        }

        private IPopup? ShowStanceTooltip(Stance stance)
        {
            var popup = _uiElementProvider?.ShowPopup(typeof(TextTooltipPopup)) as TextTooltipPopup;
            popup?.Show(
                Core.Localization.Localization.Localize($"Stance_{stance}"),
                null,
                Core.Localization.Localization.Localize($"Stance_{stance}_Description"));
            return popup;
        }

        public override void _ExitTree()
        {
            if (_abilityBook != null) _abilityBook.ActiveAbilitiesChanged -= RefreshAbilitySlots;
            _abilityBook = null;
            _battleEventBus = null;
            _characterBars.Clear();
            _queueLabels.Clear();
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
            // TODO:
            // Упростить эвент до "EntityVitalsChanges"?.
            _battleEventBus.Subscribe<PlayerMaxManaChangesEvent>(OnPlayerMaxManaChanges);
            _battleEventBus.Subscribe<PlayerMaxHealthChanges>(OnPlayerMaxHealthChanges);
            _battleEventBus.Subscribe<EntityMaxHealthChangesEvent>(OnEntityMaxHealthChanges);
            _battleEventBus.Subscribe<EntityMaxManaChangesEvent>(OnEntityMaxManaChanges);
            _battleEventBus.Subscribe<EffectsChangedEvent>(OnEffectsChanged);

            _battleEventBus.Subscribe<TurnStartEvent>(OnTurnStart);
            _battleEventBus.Subscribe<TurnEndEvent>(OnTurnEnd);
            _battleEventBus.Subscribe<PresentationStateChangedEvent>(OnPresentationStateChanged);
            _battleEventBus.Subscribe<BattleQueueDefinedEvent>(OnQueueDefined);
            // Stances are locked while an ability waits for its targets: switching mid-selection
            // would swap the ability bar under the pending cast.
            _battleEventBus.Subscribe<PlayerSelectingTargetForAbilityEvent>(OnTargetSelectionStarted);
            _battleEventBus.Subscribe<TargetSelectionResolvedEvent>(OnTargetSelectionResolved);

            foreach (AbilityButton slot in _abilitySlotsInstances)
                slot.SetBattleEventBus(_battleEventBus);
            foreach (StanceSlot stanceSlot in _stanceButtons?.GetChildren().Cast<StanceSlot>() ?? [])
                stanceSlot.SetBattleEventBus(_battleEventBus);
            _battleLog?.SetBattleEventBus(_battleEventBus);
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

        /// <summary>The card reads everything off the entity itself: vitals for the bars, the
        /// display name, the faction badge (NPCs only) and the difficulty modifier list.</summary>
        public void CreateEntityBarsWithInitialValues(IFightable entity)
        {
            var bar = CharacterBar.Initialize().Instantiate<CharacterBar>();
            bar.SetInitialValues(entity.Parameters.MaxMana, entity.CurrentMana, entity.Parameters.MaxHealth, entity.CurrentHealth,
                entity.Parameters.MaxBarrier, entity.CurrentBarrier);
            bar.SetIdentity(entity.DisplayName, entity is INpc npc ? Core.Localization.Localization.Localize($"Fraction_{npc.Fraction}") : null);
            bar.SetModifiers((entity as IFightableNpc)?.NpcModifiers.AllModifiers ?? []);
            bar.FlipH = true;
            if (entity is IFightableNpc fightableNpc) bar.SetLevel(fightableNpc.Level);
            _characterBars.Add(entity.InstanceId, bar);
            _entityBars?.AddChild(bar);
        }

        public void SetPlayerStance(Stance stance) => _stanceButtons?.GetChildren().Cast<StanceSlot>().FirstOrDefault(slot => slot.Stance == stance)?.InitializeStance();

        public void SetPlayerInitialValues(IFightable player)
        {
            _playerBars?.SetInitialValues(player.Parameters.MaxMana, player.CurrentMana, player.Parameters.MaxHealth, player.CurrentHealth,
                player.Parameters.MaxBarrier, player.CurrentBarrier);
            _playerBars?.SetIdentity(player.DisplayName, null);
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
            bar.UpdateMaxHealth(vitals.MaxHealth);
            bar.UpdateMana(vitals.Mana);
            bar.UpdateMaxMana(vitals.MaxMana);
            bar.UpdateBarrier(vitals.Barrier, vitals.MaxBarrier);
            if (vitals.IsDead) bar.SetDead();
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

        /// <summary>Round order as grey labels: the acting fighter is lit, the rest are dimmed.</summary>
        private void OnQueueDefined(BattleQueueDefinedEvent obj)
        {
            if (_queueContainer == null) return;
            _queueLabels.Clear();
            foreach (var child in _queueContainer.GetChildren())
                child.QueueFree();

            foreach (var fighter in obj.Entities)
            {
                var label = new Label { Text = fighter.DisplayName, Modulate = s_queueWaitingColor };
                _queueContainer.AddChild(label);
                _queueLabels[fighter.InstanceId] = label;
            }
        }

        private void HighlightQueue(IFightable current)
        {
            foreach (var (id, label) in _queueLabels)
                label.Modulate = current.IsSame(id) ? s_queueCurrentColor : s_queueWaitingColor;
        }

        private void OnTurnStart(TurnStartEvent obj)
        {
            _isPlayerTurn = obj.StartedTurn is IPlayer;
            if (_isPlayerTurn) _buttonsContainer?.Show();
            else _buttonsContainer?.Hide();

            HighlightQueue(obj.StartedTurn);
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
        private void OnTargetSelectionStarted(PlayerSelectingTargetForAbilityEvent evnt)
        {
            _isSelectingTargets = true;
            ApplyInputWindow();
        }

        private void OnTargetSelectionResolved(TargetSelectionResolvedEvent evnt)
        {
            _isSelectingTargets = false;
            ApplyInputWindow();
        }

        private void ApplyInputWindow()
        {
            bool open = _isPlayerTurn && !_isPresenting;
            foreach (AbilityButton slot in _abilitySlotsInstances)
                slot.SetInputEnabled(open);
            foreach (StanceSlot stanceSlot in _stanceButtons?.GetChildren().Cast<StanceSlot>() ?? [])
                stanceSlot.Disabled = !open || _isSelectingTargets; // no stance swap mid-selection
            _endTurnButton?.Disabled = !open;
            _fleeButton?.Disabled = !open;
        }
    }
}
