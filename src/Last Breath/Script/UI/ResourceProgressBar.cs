namespace Playground.Script.UI
{
    using Godot;
    using Godot.Collections;
    using Playground.Script.Enums;

    public partial class ResourceProgressBar : TextureProgressBar
    {
        [Export]
        public Dictionary<ResourceType, Texture2D> TextureByResourceType { get; set; } = [];

        public void SetResourceTexture(ResourceType type)
        {
            if(!TextureByResourceType.TryGetValue(type, out var texture))
            {
                // Log
                return;
            }
            TextureOver = texture;
        }
    }
}
