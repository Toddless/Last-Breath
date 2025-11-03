namespace LastBreath.Script.UI
{
    using Godot;
    using System;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Constants;

    public abstract partial class Window : Control, IRequireServices, IClosable
    {
        public event Action? Close;

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel) && Visible)
            {
                Close?.Invoke();
                AcceptEvent();
            }
        }

        protected void RaiseClose() => Close?.Invoke();

        public abstract void InjectServices(IGameServiceProvider provider);
    }
}
