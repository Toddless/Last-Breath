namespace LastBreath.Script.UI
{
    using Godot;
    using System;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using LastBreath.Script.Helpers;

    public partial class SaveLoadMenu : Control, IInitializable, IClosable, IRequireServices
    {
        private const string UID = "uid://cserxppd6wiui";
        [Export] private Button? _returnButton;

        public event Action? Close;

        public override void _Ready()
        {
            _returnButton!.Pressed += () => Close?.Invoke();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                Close?.Invoke();
                AcceptEvent();
            }
        }

        public void InjectServices(IGameServiceProvider provider)
        {

        }


        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
