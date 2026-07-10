namespace Battle.Source.UIElements
{
    using Core.Battle;
    using Core.Data;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The ability learning screen: mastery level with progress, every known ability per stance
    /// (learned / learnable / locked behind a mastery level) and the upgrade selection
    /// (one of three per tier, changeable freely) for learned abilities.
    /// The panel deliberately leaves the right screen edge free — AbilityUpgradeWindow docks
    /// there, so picking an ability never covers the trees.
    /// </summary>
    [GlobalClass]
    public partial class MartialArtMasteryWindow : Control, IWindow
    {
        private const string UID = "uid://ds0wq0f8ha2x5";

        [Export] private StanceTree? _dexTree, _strTree, _intTree;
        [Export] private Label? _levelLabel;
        [Export] private Label? _experienceLabel;
        [Export] private ProgressBar? _experienceBar;
        [Export] private Button? _close;

        private IMartialArtMastery? _mastery;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            _close?.Pressed += Close;
        }

        public override void _ExitTree()
        {
            if (_mastery == null) return;
            _mastery.CurrentLevelChange -= OnMasteryChanged;
            _mastery.ExperienceChange -= OnMasteryChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _dexTree?.InjectServices(provider);
            _strTree?.InjectServices(provider);
            _intTree?.InjectServices(provider);

            _mastery = provider.GetService<IMartialArtMastery>();
            _mastery.CurrentLevelChange += OnMasteryChanged;
            _mastery.ExperienceChange += OnMasteryChanged;
            RefreshProgress();
        }

        public void Close() => GetParent().RemoveChild(this);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnMasteryChanged(int value) => RefreshProgress();

        private void RefreshProgress()
        {
            if (_mastery == null) return;

            if (_levelLabel != null) _levelLabel.Text = $"Level {_mastery.CurrentLevel} / {_mastery.MaximumLevel}";

            int total = _mastery.ExpToNextLevelTotal();
            if (_experienceBar != null)
            {
                _experienceBar.MaxValue = Mathf.Max(total, 1);
                _experienceBar.Value = total == 0 ? _experienceBar.MaxValue : _mastery.CurrentExperience;
            }

            if (_experienceLabel != null)
                _experienceLabel.Text = total == 0 ? "MAX" : $"{_mastery.CurrentExperience} / {total}";
        }
    }
}
