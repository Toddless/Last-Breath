namespace Battle.Source.UIElements
{
    using Godot;
    using System.Linq;
    using Core.Interfaces.UI;
    using Core.Interfaces.Abilities;

    [GlobalClass]
    [Tool]
    public partial class CharacterBar : Control, IInitializable
    {
        private const string UID = "uid://jq1fymsa6a8n";

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
            var slot = EffectSlot.Initialize().Instantiate<EffectSlot>();
            CharacterEffects?.CallDeferred(Node.MethodName.AddChild, slot);
            slot.AddEffect(effect);
        }

        public void RemoveEffect(IEffect effect)
        {
            CharacterEffects?.GetChildren().Cast<EffectSlot>().FirstOrDefault(x => x.HasEffect(effect))?.RemoveEffect();
        }

        public void ClearEffects()
        {
            foreach (var child in CharacterEffects?.GetChildren() ?? [])
                child?.QueueFree();
        }

        public void SetInitialValues(float maxMana, float currentMana, float maxHealth, float currentHealth, Texture2D? icon = null)
        {
            ManaBar?.MaxValue = maxMana;
            ManaBar?.Value = currentMana;
            HealthBar?.MaxValue = maxHealth;
            HealthBar?.Value = currentHealth;
            if (icon != null) Icon?.Texture = icon;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void FlipElements()
        {
            if (FlipH)
            {
                LayoutDirection = LayoutDirectionEnum.Rtl;
                Icon?.FlipH = true;
                MainTexture?.FlipH = true;
                ManaBar?.FillMode = 1;
                HealthBar?.FillMode = 1;
            }
            else
            {
                LayoutDirection = LayoutDirectionEnum.Ltr;
                Icon?.FlipH = false;
                MainTexture?.FlipH = false;
                ManaBar?.FillMode = 0;
                HealthBar?.FillMode = 0;
            }
        }
    }
}
