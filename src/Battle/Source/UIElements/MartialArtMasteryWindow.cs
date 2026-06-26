namespace Battle.Source.UIElements
{
    using Core.Data;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Events;
    using Core.Interfaces.UI;
    using Godot;

    public partial class MartialArtMasteryWindow : Control, IWindow
    {
        private const string UID = "uid://ds0wq0f8ha2x5";
        private IMartialArtMastery? _mastery;
        private IGameEventBus? _gameEventBus;
        public bool IsAlreadyVisible => IsInsideTree() && IsAlreadyVisible;

        public void InjectServices(IGameServiceProvider provider)
        {
            _mastery = provider.GetService<IMartialArtMastery>();
            _gameEventBus = provider.GetService<IGameEventBus>();
            _mastery.BonusLevelChange += OnMasteryBonusLevelChanges;
            _mastery.CurrentLevelChange += OnMasteryCurrentLevelChanges;
            _mastery.ExperienceChange += OnMasteryExperienceChange;
        }

        public void Close() => GetParent().RemoveChild(this);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnMasteryExperienceChange(int amount)
        {
        }

        private void OnMasteryCurrentLevelChanges(int level)
        {
        }

        private void OnMasteryBonusLevelChanges(int bonusLevel)
        {
        }
    }
}
