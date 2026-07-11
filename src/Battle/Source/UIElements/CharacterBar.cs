namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    [GlobalClass]
    [Tool]
    public partial class CharacterBar : Control, IInitializable
    {
        private const string UID = "uid://cv5svhrugien6";
        private static readonly Color s_deadTint = new(0.45f, 0.45f, 0.45f, 0.8f);
        private Tween? _tween;
        [Export] private Control? MainContainer { get; set; }
        [Export] private ProgressBar? ManaBar { get; set; }
        [Export] private ProgressBar? HealthBar { get; set; }
        [Export] private ProgressBar? BarrierBar { get; set; }
        [Export] private TextureRect? Icon { get; set; }
        [Export] private TextureRect? MainTexture { get; set; }
        [Export] private GridContainer? CharacterEffects { get; set; }

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

        public void UpdateHealth(float value) => HealthBar?.Value = value;
        public void UpdateMana(float value) => ManaBar?.Value = value;
        public void UpdateMaxHealth(float value) => HealthBar?.MaxValue = value;
        public void UpdateMaxMana(float value) => ManaBar?.MaxValue = value;

        /// <summary>PoE-style: the barrier is a translucent overlay ON TOP of the health bar,
        /// scaled to its own maximum. Hidden while the entity carries no barrier.</summary>
        public void UpdateBarrier(float value, float maxValue)
        {
            BarrierBar?.MaxValue = Mathf.Max(maxValue, 1f);
            BarrierBar?.Value = value;
            BarrierBar?.Visible = value > 0f;
        }

        /// <summary>Grey out the fallen: the bar stays in the list, but the living roster must read at a glance.</summary>
        public void SetDead() => Modulate = s_deadTint;

        /// <summary>Reconciles the effect icons with the aggregated snapshot: one slot per effect id.</summary>
        public void SetEffects(IReadOnlyList<EffectView> effects)
        {
            var slotsById = new Dictionary<string, EffectSlot>();
            foreach (var slot in GetEffectSlots())
                if (!string.IsNullOrEmpty(slot.EffectId))
                    slotsById[slot.EffectId] = slot;

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
        }

        public void ClearEffects()
        {
            foreach (var child in CharacterEffects?.GetChildren().Cast<EffectSlot>() ?? [])
                child?.RemoveEffect();
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
            if (icon != null) Icon?.Texture = icon;
            //_tween = CreateTween();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private IEnumerable<EffectSlot> GetEffectSlots() => CharacterEffects?.GetChildren().Cast<EffectSlot>() ?? [];

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
