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

            AbilityCard card = AbilityText.Card(view);
            _abilityIcon?.Texture = view.Icon;
            _abilityName?.Text = card.Name;
            if (_meta != null)
            {
                _meta.Text = card.MetaLine;
                _meta.Visible = _meta.Text.Length > 0;
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
                node.Bind(_host, _windows);
                node.SetView(cell);
                _cellNodes.Add(node);
            }
        }

        /// <summary>The ability's own card on hover — the same one the wheel's node popup and the battle
        /// bar print. The row itself has no field for the tags, so they are shown here and only here; the
        /// meta stays the subtitle it already is beside the name.</summary>
        private IPopup? ShowAbilityTooltip()
        {
            if (_view == null || _windows == null) return null;

            AbilityCard card = AbilityText.Card(_view);
            if (card.Details.Length == 0) return null;
            if (_windows.ShowPopup(typeof(TextTooltipPopup)) is not TextTooltipPopup popup) return null;

            popup.Show(card.Name, card.MetaLine, card.Details);
            return popup;
        }
    }
}
