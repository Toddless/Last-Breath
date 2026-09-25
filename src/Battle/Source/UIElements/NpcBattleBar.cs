namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity;
    using Core.Views;
    using Godot;

    /// <summary>
    /// The over-head NPC bar: deliberately minimal — health (barrier as a
    /// thin overlay above it), mana, and +N/−N buff/debuff counters. Everything detailed lives in
    /// the hover card (<see cref="NpcInspectPopup"/>). The bar is a HUD child; the BattleHud
    /// projects it over the fighter's MODEL every frame, so it follows melee approaches and zoom.
    /// Values are replay-driven snapshots — the bar never reads live entity state after creation.
    /// </summary>
    public partial class NpcBattleBar : VBoxContainer
    {
        private const float BarWidth = 110f;

        private static readonly Color s_barrierTint = new(0.55f, 0.78f, 0.85f);
        private static readonly Color s_healthTint = new(0.72f, 0.29f, 0.26f);
        private static readonly Color s_manaTint = new(0.32f, 0.45f, 0.72f);
        private static readonly Color s_buffTint = Color.FromHtml(Core.Localization.TextPalette.Buff);
        private static readonly Color s_debuffTint = Color.FromHtml(Core.Localization.TextPalette.Debuff);

        private ProgressBar? _barrier, _health, _mana;
        private Label? _buffs, _debuffs;

        // The hover card reads these accumulated snapshots — identity is set once, vitals and
        // effects arrive with the replayed events.
        public string DisplayNameText { get; private set; } = string.Empty;
        public string? FractionText { get; private set; }
        public int Level { get; private set; }
        public IReadOnlyList<string> ModifierNames { get; private set; } = [];
        public IReadOnlyList<EffectView> Effects { get; private set; } = [];
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float Mana { get; private set; }
        public float MaxMana { get; private set; }
        public float Barrier { get; private set; }
        public float MaxBarrier { get; private set; }
        public bool IsDead { get; private set; }

        public override void _Ready()
        {
            CustomMinimumSize = new Vector2(BarWidth, 0);
            MouseFilter = MouseFilterEnum.Stop; // the hover card attaches to the whole bar
            AddThemeConstantOverride("separation", 1);

            _barrier = AddBar(3, s_barrierTint);
            _health = AddBar(7, s_healthTint);
            _mana = AddBar(4, s_manaTint);

            var counters = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            counters.AddThemeConstantOverride("separation", 6);
            _buffs = AddCounter(counters, s_buffTint);
            _debuffs = AddCounter(counters, s_debuffTint);
            AddChild(counters);

            RefreshBars();
        }

        public void SetInitialValues(float maxMana, float mana, float maxHealth, float health, float maxBarrier, float barrier)
        {
            (MaxMana, Mana, MaxHealth, Health, MaxBarrier, Barrier) = (maxMana, mana, maxHealth, health, maxBarrier, barrier);
            RefreshBars();
        }

        public void SetIdentity(string displayName, string? fraction)
        {
            DisplayNameText = displayName;
            FractionText = fraction;
        }

        public void SetModifiers(IReadOnlyList<INpcModifier> modifiers) =>
            ModifierNames = modifiers.Select(modifier => modifier.DisplayName).ToList();

        public void SetLevel(int level) => Level = level;

        public void SetEffects(IReadOnlyList<EffectView> effects)
        {
            Effects = effects;
            int buffs = effects.Count(view => !view.IsHarmful);
            int debuffs = effects.Count - buffs;
            _buffs?.Text = buffs > 0 ? $"+{buffs}" : string.Empty;
            _debuffs?.Text = debuffs > 0 ? $"-{debuffs}" : string.Empty;
        }

        public void UpdateHealth(float value) { Health = value; RefreshBars(); }
        public void UpdateMaxHealth(float value) { MaxHealth = value; RefreshBars(); }
        public void UpdateMana(float value) { Mana = value; RefreshBars(); }
        public void UpdateMaxMana(float value) { MaxMana = value; RefreshBars(); }

        public void UpdateBarrier(float value, float max)
        {
            (Barrier, MaxBarrier) = (value, max);
            RefreshBars();
        }

        public void SetDead()
        {
            IsDead = true;
            Modulate = new Color(1f, 1f, 1f, 0.35f);
        }

        private void RefreshBars()
        {
            SetBar(_health, Health, MaxHealth);
            SetBar(_mana, Mana, MaxMana);
            SetBar(_barrier, Barrier, MaxBarrier);
            _barrier?.Visible = MaxBarrier > 0 && Barrier > 0;
        }

        private static void SetBar(ProgressBar? bar, float value, float max)
        {
            if (bar == null) return;
            bar.MaxValue = Mathf.Max(max, 1f);
            bar.Value = value;
        }

        private ProgressBar AddBar(int height, Color tint)
        {
            var bar = new ProgressBar
            {
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(BarWidth, height),
                MouseFilter = MouseFilterEnum.Ignore,
                SelfModulate = tint,
            };
            AddChild(bar);
            return bar;
        }

        private static Label AddCounter(Node parent, Color tint)
        {
            var label = new Label { MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeColorOverride("font_color", tint);
            label.AddThemeFontSizeOverride("font_size", 12);
            parent.AddChild(label);
            return label;
        }
    }
}
