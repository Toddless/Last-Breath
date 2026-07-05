namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.UI;
    using Core.Views;
    using Godot;

    [GlobalClass]
    [Tool]
    public partial class CharacterBar : Control, IInitializable
    {
        private const string UID = "uid://cv5svhrugien6";
        private Tween? _tween;
        [Export] private Control? MainContainer { get; set; }
        [Export] private TextureProgressBar? ManaBar { get; set; }
        [Export] private TextureProgressBar? HealthBar { get; set; }
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

        /// <summary>Reconciles the effect icons with the aggregated snapshot: one slot per effect id.</summary>
        public void SetEffects(IReadOnlyList<EffectView> effects)
        {
            var slotsById = new Dictionary<string, EffectSlot>();
            foreach (var slot in GetEffectSlots())
                if (!string.IsNullOrEmpty(slot.EffectId)) slotsById[slot.EffectId] = slot;

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
                if (!activeIds.Contains(id)) slot.RemoveEffect();
        }

        public void ClearEffects()
        {
            foreach (var child in CharacterEffects?.GetChildren().Cast<EffectSlot>() ?? [])
                child?.RemoveEffect();
        }

        public void SetInitialValues(float maxMana, float currentMana, float maxHealth, float currentHealth, Texture2D? icon = null)
        {
            ManaBar?.MaxValue = maxMana;
            ManaBar?.Value = currentMana;
            HealthBar?.MaxValue = maxHealth;
            HealthBar?.Value = currentHealth;
            if (icon != null) Icon?.Texture = icon;
            //_tween = CreateTween();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private IEnumerable<EffectSlot> GetEffectSlots() => CharacterEffects?.GetChildren().Cast<EffectSlot>() ?? [];

        private void AnimateValueChange(TextureProgressBar progressBar, float newValue, bool isMaxValue = false)
        {
            string propertyName = isMaxValue ? "max_value" : "value";
            _tween?.TweenProperty(progressBar, propertyName, isMaxValue ? progressBar.MaxValue : progressBar.Value, newValue);
        }

        private void FlipElements()
        {
            LayoutDirection = FlipH ? LayoutDirectionEnum.Rtl : LayoutDirectionEnum.Ltr;
            Icon?.FlipH = FlipH;
            MainTexture?.FlipH = FlipH;
            ManaBar?.FillMode = FlipH ? 1 : 0;
            HealthBar?.FillMode = FlipH ? 1 : 0;
        }
    }
}
