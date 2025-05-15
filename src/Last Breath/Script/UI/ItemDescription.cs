namespace Playground.Script.UI
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Helpers;

    public partial class ItemDescription : MarginContainer
    {
        [Export] private TextureRect? _itemImage;
        [Export] private VBoxContainer? _statsBox, _descriptionBox;

        public void SetStats(IEnumerable<string> stats)
        {
            foreach (var stat in stats)
            {
                var label = new Label() { Text = stat };
                _statsBox?.AddChild(label);
            }
        }

        public void SetItemImage(Texture2D? texture)
        {
            if (texture == null || _itemImage == null) return; 
            _itemImage.Texture = texture;
        }

        public void SetItemDescription(string text)
        {
            var desc = new RichTextLabel
            {
                Text = text
            };
            _descriptionBox?.AddChild(desc);
        }

        public static PackedScene InitializeAsPackedScene() => ResourceLoader.Load<PackedScene>(ScenePath.ItemDescription);
    }
}
