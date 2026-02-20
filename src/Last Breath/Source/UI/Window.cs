namespace LastBreath.Source.UI
{
    using System;
    using Core.Constants;
    using Core.Data;
    using Core.Interfaces.UI;
    using Godot;

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
