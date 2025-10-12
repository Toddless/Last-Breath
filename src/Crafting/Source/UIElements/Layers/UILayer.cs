namespace Crafting.Source.UIElements.Layers
{
    using Godot;
    using Core.Interfaces.Mediator;
    using Crafting.TestResources.DI;

    public partial class UILayer : CanvasLayer
    {
        public override void _Ready()
        {
            ServiceProvider.Instance.GetService<IUiMediator>().Subscribe(this);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_crafting"))
            {
                if (Visible) Hide();
                else Show();
            }
        }

        public void ShowWindow(Control control) => CallDeferred(MethodName.AddChild, control);
    }
}
