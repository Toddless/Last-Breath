namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// One combatant's card in the battle HUD: framed portrait, name with an optional faction
    /// badge, health bar with the barrier overlaid PoE-style and the numbers on top, mana bar,
    /// then ruled sections for active effects (stacked icons) and the NPC difficulty modifiers.
    /// Sections collapse when empty; the player's card simply never gets a faction or modifiers.
    /// </summary>
    [GlobalClass]
    [Tool]
    public partial class CharacterBar : PanelContainer, IInitializable
    {
        private const string UID = "uid://cv5svhrugien6";
        private const int ModifierLineWidth = 300;
        private static readonly Color s_deadTint = new(0.45f, 0.45f, 0.45f, 0.8f);
        private Tween? _tween;
        [Export] private Control? MainContainer { get; set; }
        [Export] private ProgressBar? ManaBar { get; set; }
        [Export] private ProgressBar? HealthBar { get; set; }
        [Export] private ProgressBar? BarrierBar { get; set; }
        [Export] private TextureRect? Icon { get; set; }
        [Export] private TextureRect? MainTexture { get; set; }
        [Export] private GridContainer? CharacterEffects { get; set; }
        [Export] private Label? _name;
        [Export] private Control? _factionBadge;
        [Export] private Label? _factionLabel;
        [Export] private Label? _hpText;
        [Export] private Label? _manaText;
        [Export] private Label? _level;
        [Export] private Control? _effectsSection;
        [Export] private Label? _effectsHeaderLabel;
        [Export] private Control? _modsSection;
        [Export] private Label? _modsHeaderLabel;
        [Export] private VBoxContainer? _mods;

        [Export]
        public bool FlipH
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                FlipElements();
            }
        }

        public override void _Ready()
        {
            if (Engine.IsEditorHint()) return;
            _effectsHeaderLabel?.Text = Core.Localization.Localization.Localize("UI_Bar_Effects").ToUpper();
            _modsHeaderLabel?.Text = Core.Localization.Localization.Localize("UI_Bar_Modifiers").ToUpper();
        }

        public void UpdateHealth(float value)
        {
            HealthBar?.Value = value;
            RefreshVitalTexts();
        }

        public void UpdateMana(float value)
        {
            ManaBar?.Value = value;
            RefreshVitalTexts();
        }

        public void UpdateMaxHealth(float value)
        {
            HealthBar?.MaxValue = value;
            RefreshVitalTexts();
        }

        public void UpdateMaxMana(float value)
        {
            ManaBar?.MaxValue = value;
            RefreshVitalTexts();
        }

        /// <summary>PoE-style: the barrier is a translucent overlay ON TOP of the health bar,
        /// scaled to its own maximum. Hidden while the entity carries no barrier.</summary>
        public void UpdateBarrier(float value, float maxValue)
        {
            BarrierBar?.MaxValue = Mathf.Max(maxValue, 1f);
            BarrierBar?.Value = value;
            BarrierBar?.Visible = value > 0f;
        }

        public void SetLevel(int level) => _level?.Text = level.ToString();

        /// <summary>Grey out the fallen: the bar stays in the list, but the living roster must read at a glance.</summary>
        public void SetDead() => Modulate = s_deadTint;

        /// <summary>Name plus the faction badge; no faction (the player, summons) hides the badge.</summary>
        public void SetIdentity(string name, string? faction)
        {
            _name?.Text = name;
            _factionLabel?.Text = faction ?? string.Empty;
            _factionBadge?.Visible = !string.IsNullOrEmpty(faction);
        }

        /// <summary>The NPC difficulty modifier rows; empty list collapses the section.</summary>
        public void SetModifiers(IReadOnlyList<INpcModifier> modifiers) =>
            SetModifiers(modifiers.Select(modifier => modifier.DisplayName).ToList());

        /// <summary>Snapshot flavour: the NPC inspect card carries only the display names.</summary>
        public void SetModifiers(IReadOnlyList<string> modifierNames)
        {
            if (_mods == null || _modsSection == null) return;

            foreach (var child in _mods.GetChildren())
                child.QueueFree();

            foreach (string text in modifierNames)
            {
                _mods.AddChild(new Label
                {
                    Text = text,
                    ThemeTypeVariation = "DimLabel",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    CustomMinimumSize = new Vector2(ModifierLineWidth, 0),
                });
            }

            _modsSection.Visible = modifierNames.Count > 0;
        }

        /// <summary>Reconciles the effect icons with the aggregated snapshot: one slot per effect id.</summary>
        public void SetEffects(IReadOnlyList<EffectView> effects)
        {
            var slotsById = new Dictionary<string, EffectSlot>();
            foreach (var slot in GetEffectSlots())
                if (!string.IsNullOrEmpty(slot.EffectId))
                    slotsById[slot.EffectId] = slot;

            // TODO:
            // отображения бесконечных эффектов
            foreach (var view in effects)
            {
                if (slotsById.TryGetValue(view.Id, out var slot))
                {
                    slot.SetView(view);
                    continue;
                }

                var newSlot = EffectSlot.Initialize().Instantiate<EffectSlot>();
                CharacterEffects?.AddChild(newSlot);
                newSlot.SetView(view);
                slotsById[view.Id] = newSlot;
            }

            var activeIds = effects.Select(view => view.Id).ToHashSet();
            foreach (var (id, slot) in slotsById)
                if (!activeIds.Contains(id))
                    slot.RemoveEffect();

            _effectsSection?.Visible = effects.Count > 0;
        }

        public void ClearEffects()
        {
            foreach (var child in CharacterEffects?.GetChildren().Cast<EffectSlot>() ?? [])
                child?.RemoveEffect();
            _effectsSection?.Visible = false;
        }

        public void SetInitialValues(float maxMana, float currentMana, float maxHealth, float currentHealth, float maxBarrier = 0f, float currentBarrier = 0f, Texture2D? icon = null)
        {
            ManaBar?.MaxValue = maxMana;
            ManaBar?.Value = currentMana;
            HealthBar?.MaxValue = maxHealth;
            HealthBar?.Value = currentHealth;
            // The scene's defaults must never leak: a bar created mid-battle (a latecomer) may not
            // receive a vitals snapshot for a whole round.
            UpdateBarrier(currentBarrier, maxBarrier);
            RefreshVitalTexts();
            if (icon != null) Icon?.Texture = icon;
            //_tween = CreateTween();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private IEnumerable<EffectSlot> GetEffectSlots() => CharacterEffects?.GetChildren().Cast<EffectSlot>() ?? [];

        private void RefreshVitalTexts()
        {
            if (_hpText != null && HealthBar != null)
                _hpText.Text = $"{Mathf.CeilToInt(HealthBar.Value)} / {Mathf.CeilToInt(HealthBar.MaxValue)}";
            if (_manaText != null && ManaBar != null)
                _manaText.Text = $"{Mathf.CeilToInt(ManaBar.Value)} / {Mathf.CeilToInt(ManaBar.MaxValue)}";
        }

        private void AnimateValueChange(ProgressBar progressBar, float newValue, bool isMaxValue = false)
        {
            string propertyName = isMaxValue ? "max_value" : "value";
            _tween?.TweenProperty(progressBar, propertyName, isMaxValue ? progressBar.MaxValue : progressBar.Value, newValue);
        }

        private void FlipElements()
        {
            LayoutDirection = FlipH ? LayoutDirectionEnum.Rtl : LayoutDirectionEnum.Ltr;
            Icon?.FlipH = FlipH;
            MainTexture?.FlipH = FlipH;
            ManaBar?.FillMode = FlipH ? (int)ProgressBar.FillModeEnum.EndToBegin : (int)ProgressBar.FillModeEnum.BeginToEnd;
            HealthBar?.FillMode = FlipH ? (int)ProgressBar.FillModeEnum.EndToBegin : (int)ProgressBar.FillModeEnum.BeginToEnd;
            BarrierBar?.FillMode = FlipH ? (int)ProgressBar.FillModeEnum.EndToBegin : (int)ProgressBar.FillModeEnum.BeginToEnd;
        }
    }
}
