namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Interfaces.Data;
    using LastBreath.Script.Helpers;

    public abstract partial class Window : Control, IRequireServices
    {

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel) && Visible)
            {
                Hide();
                AcceptEvent();
            }
        }

        public abstract void InjectServices(IGameServiceProvider provider);
    }
}
