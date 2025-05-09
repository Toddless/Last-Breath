namespace Playground.Script.Items.ItemData
{
    using Godot;
    using Playground.Localization;

    public class ItemMediaData
    {
        public Texture2D? Texture { get; set; }
        public LocalizedString? Description { get; set; }
        public LocalizedString? Name { get; set; }
        public AudioStream? Sound { get; set; }
    }
}
