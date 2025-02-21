namespace Playground.Script.UI.Layers
{
    using Godot;
    using Playground.Script.NPC;
    using Playground.Script.UI.View;

    public partial class DialogLayer : CanvasLayer
    {
        private DialogWindow? _dialogWindow;

        public override void _Ready()
        {
            _dialogWindow = GetNode<DialogWindow>(nameof(DialogWindow));
            SetEvents();
        }

        internal void SetupWindow(BaseNPC npc)
        {
            _dialogWindow.SetAvatar(GameManager.Instance.Player.PlayerAvatar.Texture);
            _dialogWindow.UpdateText("Hallo there! Are you new here? I did`t see you bevor.");
        }

        private void SetEvents()
        {

        }
    }
}
