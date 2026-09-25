namespace LastBreath.World.Interactions.UI
{
    using System.Linq;
    using Core.Localization;
    using Core.Views.UI;
    using Core.World.Interactions;
    using Godot;

    public partial class InteractionHint : PanelContainer
    {
        private InteractionService _service = null!;
        [Export] private Label? _label;
        public override void _Ready()
        {
            _service = Services.GameServiceProvider.Instance.GetService<InteractionService>();
            MouseFilter = MouseFilterEnum.Ignore;
            Hide();
        }

        public override void _Process(double delta)
        {
            if (_service.Selected is not { } target || !GodotObject.IsInstanceValid(target) || !target.IsInsideTree())
            { Hide(); return; }
            var actions = _service.SelectedActions;
            var enabled = actions.Where(x => x.Enabled).ToList();
            string key = enabled.Count == 1 ? enabled[0].LabelKey : InteractionPresentation.InteractKey;
            _label?.Text = enabled.Count > 0
                ? InteractionPresentation.Prompt(InteractionActions.Interact, key)
                : Localization.Localize(actions.FirstOrDefault()?.ReasonKey ?? InteractionReasonKeys.Unavailable);
            Place(InteractionPresentation.ScreenPoint(target.Anchor) - new Vector2(Size.X / 2, Size.Y));
        }

        /// <summary>A hidden hint appears already standing at the point; a visible one follows it without toggling visibility.</summary>
        private void Place(Vector2 point)
        {
            if (Visible) UiPlacement.Follow(this, point);
            else UiPlacement.PlaceClamped(this, point);
        }
    }
}
