namespace Battle.Source.UIElements
{
    using System;
    using System.Linq;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;
    using Core.PassiveTree.Allocation;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The mastery screen: the level and its experience above, and below it the socket sheet — one
    /// section per stance, each an unbroken list of the abilities the player OWNS with their augment
    /// slots beside them. Abilities he has not unlocked are not on it at all; they are the passive
    /// wheel's business, and that is a screen of its own.
    /// Nothing here counts rows, cells or stances: the sections are an exported array, the rows come
    /// from what the player has learned and the cells from what his allocation opened, so a fourth
    /// stance or a fourth slot is a change to the scene and not to this file.
    /// </summary>
    [GlobalClass]
    public partial class MartialArtMasteryWindow : Control, IWindow
    {
        private const string UID = "uid://ds0wq0f8ha2x5";

        [Export] private StanceSocketSection[] _sections = [];
        [Export] private AugmentTray? _tray;
        [Export] private Label? _levelLabel;
        [Export] private Label? _experienceLabel;
        [Export] private ProgressBar? _experienceBar;
        [Export] private Label? _pointsLabel;
        [Export] private Label? _waitingLabel;
        [Export] private Button? _close;

        private IMartialArtMastery? _mastery;
        private IPassiveTreeService? _tree;
        private IAbilitySocketBoard? _board;

        public override void _Ready()
        {
            _close?.Pressed += Close;
        }

        public override void _ExitTree()
        {
            if (_mastery != null)
            {
                _mastery.CurrentLevelChange -= OnMasteryChanged;
                _mastery.ExperienceChange -= OnMasteryChanged;
            }

            if (_tree != null) _tree.AllocationChanged -= RefreshAllocation;
            if (_board != null) _board.Changed -= RefreshAllocation;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            foreach (StanceSocketSection section in _sections) section.InjectServices(provider);
            ReportMissingSections();
            _tray?.InjectServices(provider);

            _mastery = provider.GetService<IMartialArtMastery>();
            _mastery.CurrentLevelChange += OnMasteryChanged;
            _mastery.ExperienceChange += OnMasteryChanged;

            // Optional: the battle sandbox composes no passive tree, and a mastery screen that took the
            // scene down there would be worse than one showing no point counter.
            _tree = provider.Optional<IPassiveTreeService>();
            if (_tree != null) _tree.AllocationChanged += RefreshAllocation;
            _board = provider.GetService<IAbilitySocketBoard>();
            _board.Changed += RefreshAllocation;

            RefreshProgress();
            RefreshAllocation();
        }

        // Close = death (QueueFree), the IWindow contract: RemoveChild left a live, still-tracked
        // node parentless, so a following Esc (CloseDismissableWindows) called Close() again on it
        // and GetParent() was null -> NRE. Same fix as the save/options windows (bugs #27/#29).
        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>A stance with no section in the scene would simply be invisible, and nothing else
        /// would say so. Checked here because the sections are the scene's to lay out and this is the
        /// one place that knows the whole set.</summary>
        private void ReportMissingSections()
        {
            foreach (Stance stance in Enum.GetValues<Stance>())
                if (_sections.All(section => section.Stance != stance))
                    Tracker.TrackNotFound($"Mastery window has no socket section for stance '{stance}'", this);
        }

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

        /// <summary>The two numbers that move with the tree: points still to spend, and how many
        /// augments are sitting in slots no node backs any more. The second one is what keeps the rule
        /// "nothing of the player's is ever lost" from being true only on paper — without it he never
        /// learns there is property in the dimmed cells waiting to be taken out.</summary>
        private void RefreshAllocation()
        {
            if (_pointsLabel != null)
            {
                _pointsLabel.Visible = _tree != null;
                if (_tree != null) _pointsLabel.Text = _tree.AvailablePoints.ToString();
            }

            if (_waitingLabel == null || _board == null) return;

            int waiting = _board.Sockets.Count(socket => !socket.IsOpen && !socket.IsEmpty);
            _waitingLabel.Text = waiting.ToString();
            _waitingLabel.Visible = waiting > 0;
        }
    }
}
