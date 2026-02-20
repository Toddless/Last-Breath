namespace Battle.Source.UIElements
{
    using Godot;
    using System.Linq;
    using Core.Interfaces.UI;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

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

        public void AddEffect(IEffect effect)
        {
            var slots = GetEffectSlots().ToList();
            var alreadyExistInSlot = slots.FirstOrDefault(x => x.HasEffect(effect));
            if (alreadyExistInSlot == null)
            {
                var slot = EffectSlot.Initialize().Instantiate<EffectSlot>();
                CharacterEffects?.CallDeferred(Node.MethodName.AddChild, slot);
                slot.AddEffect(effect);
                slot.Stacks++;
            }
            else
            {
                if (!alreadyExistInSlot.HasOwner())
                    alreadyExistInSlot.AddEffect(effect);
                alreadyExistInSlot.Stacks++;
            }
        }

        public void RemoveEffect(IEffect effect)
        {
            var slots = GetEffectSlots().ToList();
            var existing = slots.FirstOrDefault(x => x.HasEffect(effect));
            if (existing == null) return;

            if (existing.Stacks > 1) existing.Stacks--;
            else existing.RemoveEffect();
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
            _tween = new Tween();
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
