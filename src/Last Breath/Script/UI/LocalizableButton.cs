namespace LastBreath.Script.UI
{
    using Godot;
    using Utilities;

    [GlobalClass]
    public partial class LocalizableButton : Button
    {
        private string _id = string.Empty;

        public override void _Ready()
        {
            _id = Text;

            Text = Localizator.Localize(_id);
        }

        public void UpdateButtonText() => Text = Localizator.Localize(_id);
    }
}
