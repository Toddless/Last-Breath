namespace LastBreath.World.Interactions.UI
{
    using Core.Data;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Views.UI;
    using Godot;

    public abstract partial class InteractionWindow : Control, IWindow
    {
        private static readonly Vector2 PanelOffset = new(24, 12);
        private AudioStreamPlayer _refusal = null!;
        protected InteractionService? Service;
        protected InteractionTarget? Target;
        protected IGameMessageBus Messages = null!;
        protected VBoxContainer Rows = null!;
        protected PanelContainer Panel = null!;
        protected Button CloseButton = null!;
        /// <summary>The bound target while it is alive in the scene tree; null otherwise.</summary>
        private InteractionTarget? LiveTarget => Target != null && GodotObject.IsInstanceValid(Target) && Target.IsInsideTree() ? Target : null;
        public bool BlocksMovement => true;

        public void InjectServices(IGameServiceProvider provider) => Messages = provider.GetService<IGameMessageBus>();
        public override void _Ready()
        {
            Panel = GetNode<PanelContainer>("Frame");
            Rows = GetNode<VBoxContainer>("Frame/Content/Rows");
            CloseButton = GetNode<Button>("Frame/Content/Close");
            CloseButton.Text = Localization.Localize("UI_Close");
            CloseButton.Pressed += Close;
            _refusal = GetNode<AudioStreamPlayer>("Refusal");
            if (LiveTarget is not { } target) return;
            // _Ready precedes the first drawn frame; placing before Refresh keeps the hide from dropping the rows' focus.
            UiPlacement.PlaceClamped(Panel, InteractionPresentation.ScreenPoint(target.Anchor), PanelOffset);
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
            UiPlacement.Follow(Panel, InteractionPresentation.ScreenPoint(target.Anchor), PanelOffset);
        }
        public virtual void Close()
        {
            Service?.WindowClosed(this);
            QueueFree();
        }
        public override void _ExitTree() => Service?.WindowClosed(this);
        public abstract void Refresh();
        protected void ClearRows()
        {
            for (int i = Rows.GetChildCount() - 1; i >= 0; i--)
            {
                var child = Rows.GetChild(i);
                Rows.RemoveChild(child);
                child.QueueFree();
            }
        }
        public void CapacityRefused()
        {
            _refusal.Play();
            _ = Messages.PublishMessageAsync(new SendNotificationMessageMessage("UI_Container_Full"));
        }
    }
}
