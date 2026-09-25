namespace LastBreath.World.Interactions.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Constants;
    using Core.Localization;
    using Core.Views.UI;
    using Core.World.Interactions;
    using Godot;

    public partial class InteractionHint : PanelContainer
    {
        private InteractionService _service = null!;
        [Export] private Label? _label;
        /// <summary>Selected target the label text was last built for.</summary>
        private InteractionTarget? _shownTarget;
        /// <summary>Action list the label text was last built from.</summary>
        private IReadOnlyList<InteractionAction>? _shownActions;
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
            RefreshText(target, _service.SelectedActions);
            Place(InteractionPresentation.ScreenPoint(target.Anchor) - new Vector2(Size.X / 2, Size.Y));
        }

        /// <summary>Rebuilds the label text when the target or its action list is not the one the text was last built for.</summary>
        private void RefreshText(InteractionTarget target, IReadOnlyList<InteractionAction> actions)
        {
            if (ReferenceEquals(target, _shownTarget) && ReferenceEquals(actions, _shownActions)) return;
            _shownTarget = target;
            _shownActions = actions;
            _label?.Text = TextFor(actions);
        }

        /// <summary>Text by the enabled count: none — the first action's reason, else Unavailable; one — its prompt; several — the Interact prompt.</summary>
        private static string TextFor(IReadOnlyList<InteractionAction> actions)
        {
            var enabled = actions.Where(x => x.Enabled).ToList();
            return enabled.Count switch
            {
                0 => Localization.Localize(actions.FirstOrDefault()?.ReasonKey ?? InteractionReasonKeys.Unavailable),
                1 => InteractionPresentation.Prompt(Settings.Interact, enabled[0].LabelKey),
                _ => InteractionPresentation.Prompt(Settings.Interact, InteractionPresentation.InteractKey),
            };
        }

        /// <summary>A hidden hint appears already standing at the point; a visible one follows it without toggling visibility.</summary>
        private void Place(Vector2 point)
        {
            if (Visible) UiPlacement.Follow(this, point);
            else UiPlacement.PlaceClamped(this, point);
        }
    }
}
