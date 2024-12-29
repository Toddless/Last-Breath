namespace Playground.Script.Passives
{
    using Godot;

    public abstract class BaseDebuff
    {
        private Texture2D? _icon;
        private int _duration;
        private string? _description;


        public Texture2D? Icon
        {
            get => _icon;
            set => _icon = value;
        }

        public int Duration
        {
            get => _duration;
            set => _duration = value;
        }

        public string? Description
        {
            get => _description;
            set => _description = value;
        }
    }
}
