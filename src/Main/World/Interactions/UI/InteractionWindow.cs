namespace LastBreath.World.Interactions.UI
{
    using Core.Data;
    using Core.Localization;
    using Core.MessageBus;
    using Core.Views.UI;
    using Core.World.Interactions;
    using Godot;

    public abstract partial class InteractionWindow : Control, IWindow, IInteractionSession
    {
        private const string CloseKey = "UI_Close";
        private static readonly Vector2 PanelOffset = new(24, 12);
        private bool _ended;
        protected InteractionService? Service;
        protected IGameMessageBus Messages = null!;
        [Export] protected VBoxContainer? Rows;
        [Export] protected PanelContainer? Panel;
        [Export] protected Button? CloseButton;
        [Export] protected Label? Title;
        /// <summary>The bound target while it is alive in the scene tree; null otherwise.</summary>
        private InteractionTarget? LiveTarget => IsInstanceValid(Target) && Target.IsInsideTree() ? Target : null;
        /// <summary>Target this window is bound to by <see cref="Bind"/>.</summary>
        public InteractionTarget Target { get; private set; } = null!;
        /// <summary>The window is the session's own UI.</summary>
        IWindow IInteractionSession.Window => this;
        /// <summary>Open while the node is valid and not queued for deletion.</summary>
        public bool IsOpen => IsInstanceValid(this) && !IsQueuedForDeletion();
        public bool BlocksMovement => true;

        public virtual void InjectServices(IGameServiceProvider provider) => Messages = provider.GetService<IGameMessageBus>();
        public override void _Ready()
        {
            CloseButton?.Text = Localization.Localize(CloseKey);
            CloseButton?.Pressed += Close;
            if (LiveTarget is not { } target) return;
            // _Ready precedes the first drawn frame; placing before Refresh keeps the hide from dropping the rows' focus.
            if (Panel != null) UiPlacement.PlaceClamped(Panel, InteractionPresentation.ScreenPoint(target.Anchor), PanelOffset);
            Refresh();
        }
        public void Bind(InteractionService service, InteractionTarget target)
        {
            Service = service;
            Target = target;
            if (IsNodeReady()) Refresh();
        }
        public override void _Process(double delta)
        {
            if (LiveTarget is not { } target) { Close(); return; }
            if (Panel != null) UiPlacement.Follow(Panel, InteractionPresentation.ScreenPoint(target.Anchor), PanelOffset);
        }
        /// <summary>Closes the window whatever the cause, without reporting back to the service; later calls do nothing.</summary>
        public void End(InteractionSessionEndCause cause)
        {
            if (_ended) return;
            _ended = true;
            if (IsOpen) Close();
        }
        /// <summary>Closes the window; closing on its own terms reports the ended session to the service.</summary>
        public virtual void Close()
        {
            ReportClosed();
            QueueFree();
        }
        public override void _ExitTree() => ReportClosed();
        public abstract void Refresh();
        protected void ClearRows()
        {
            if (Rows == null) return;
            for (int i = Rows.GetChildCount() - 1; i >= 0; i--)
            {
                var child = Rows.GetChild(i);
                Rows.RemoveChild(child);
                child.QueueFree();
            }
        }
        /// <summary>Tells the service once that the session ended on the window's own terms; silent after <see cref="End"/>.</summary>
        private void ReportClosed()
        {
            if (_ended) return;
            _ended = true;
            Service?.SessionClosed(this);
        }
    }
}
