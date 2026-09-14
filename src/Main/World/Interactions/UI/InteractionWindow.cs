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
        protected InteractionService? Service;
        protected InteractionTarget? Target;
        protected IGameMessageBus Messages = null!;
        protected VBoxContainer Rows = null!;
        protected PanelContainer Panel = null!;
        private AudioStreamPlayer _refusal = null!;
        public bool BlocksMovement => true;

        public void InjectServices(IGameServiceProvider provider) => Messages = provider.GetService<IGameMessageBus>();
        public override void _Ready()
        {
            Panel = GetNode<PanelContainer>("Frame");
            Rows = GetNode<VBoxContainer>("Frame/Content/Rows");
            GetNode<Button>("Frame/Content/Close").Text = Localization.Localize("UI_Close");
            GetNode<Button>("Frame/Content/Close").Pressed += Close;
            _refusal = GetNode<AudioStreamPlayer>("Refusal");
            if (Target != null) Refresh();
        }
        public void Bind(InteractionService service, InteractionTarget target)
        {
            Service = service;
            Target = target;
            if (IsNodeReady()) Refresh();
        }
        public override void _Process(double delta)
        {
            if (Target == null || !GodotObject.IsInstanceValid(Target) || !Target.IsInsideTree()) { Close(); return; }
            UiPlacement.PlaceClamped(Panel, InteractionPresentation.ScreenPoint(Target.Anchor), new Vector2(24, 12));
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
