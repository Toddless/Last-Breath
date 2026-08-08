namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// One ability of the socket sheet: its icon and name on the left, its slots in a row beside it.
    /// How many slots there are is the board's answer and never the scene's — an ability owns as many
    /// as the allocation opened for it, three today and four with an ornament, and nothing here counts.
    ///
    /// A row whose ability the player no longer owns is dimmed whole: it is on screen only because his
    /// augments are still in its slots, and it leaves when he has taken the last one out.
    /// </summary>
    [GlobalClass]
    public partial class AbilitySocketRow : PanelContainer
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://cx8hj2lm5ta0d";

        private static readonly Color s_unownedTint = new(0.55f, 0.55f, 0.55f);

        /// <summary>The cells as they were built, so a drag can light them without asking the engine
        /// for a child list that still holds the ones queued for deletion.</summary>
        private readonly List<AugmentCell> _cellNodes = [];

        [Export] private TextureRect? _abilityIcon;
        [Export] private Label? _abilityName;
        [Export] private Label? _meta;
        [Export] private HBoxContainer? _cells;
        [Export] private PackedScene? _cellScene;

        private AbilitySocketRowView? _view;
        private IAugmentCellHost? _host;
        private IUiElementsManager? _windows;

        public IReadOnlyList<AugmentCell> Cells => _cellNodes;

        public override void _Ready()
        {
            if (_abilityIcon != null) HoverTooltip.Attach(_abilityIcon, ShowAbilityTooltip);
        }

        /// <summary>Fills the row and rebuilds its cells from scratch. Rebuilt rather than diffed: the
        /// number of slots changes with the allocation, and a row that kept cells from the previous set
        /// is a row able to show a slot that no longer exists.</summary>
        public void SetView(AbilitySocketRowView view, IAugmentCellHost host, IUiElementsManager? windows)
        {
            _view = view;
            _host = host;
            _windows = windows;

            _abilityIcon?.Texture = view.Icon;
            _abilityName?.Text = view.DisplayName;
            if (_meta != null)
            {
                _meta.Text = $"{view.Cost} {view.Cooldown}".Trim();
                _meta.Visible = !string.IsNullOrWhiteSpace(_meta.Text);
            }

            Modulate = view.IsOwned ? Colors.White : s_unownedTint;
            BuildCells(view);
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        private void BuildCells(AbilitySocketRowView view)
        {
            _cellNodes.Clear();
            if (_cells == null || _cellScene == null || _host == null) return;

            _cells.QueueFreeChildren();
            foreach (AugmentCellView cell in view.Cells)
            {
                var node = _cellScene.Instantiate<AugmentCell>();
                _cells.AddChild(node);
                node.Bind(_host);
                node.SetView(cell);
                _cellNodes.Add(node);
            }
        }

        /// <summary>The ability's own text on hover. The owner asked for no detail arrow, but the
        /// description is what the numbers in the row are about and must stay reachable.</summary>
        private IPopup? ShowAbilityTooltip()
        {
            if (_view == null || _windows == null || string.IsNullOrEmpty(_view.Description)) return null;
            if (_windows.ShowPopup(typeof(TextTooltipPopup)) is not TextTooltipPopup popup) return null;

            popup.Show(_view.DisplayName, _meta?.Text, _view.Description);
            return popup;
        }
    }
}
