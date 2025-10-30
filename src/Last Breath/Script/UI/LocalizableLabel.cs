namespace LastBreath.Script.UI
{
    using Godot;
    using Utilities;

    [GlobalClass]
    public partial class LocalizableLabel : Label
    {
        private string _id = string.Empty;

        public override void _Ready()
        {
            _id = Text;
            Text = Localizator.Localize(_id);
        }

        public void UpdateLabelText() => Text = Localizator.Localize(_id);
    }
}
