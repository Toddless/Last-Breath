namespace LastBreath.Script.Inventory
{
    using Godot;
    using LastBreath.Script.UI;

    public abstract partial class Slot: TextureButton
    {
        private const string MainLayer = "Main/MainLayer";
        protected ItemDescription? ItemDescription;

        [Export] protected Texture2D? DefaltTexture;

        public string? CurrentItem { get; protected set; }

        protected void OnMouseExit()
        {
            if (ItemDescription != null)
            {
                ItemDescription.QueueFree();
                ItemDescription = null;
            }
        }

        protected void OnMouseEnter()
        {
        //    if (CurrentItem == null) return;
        //    var description = ItemDescription.InitializeAsPackedScene().Instantiate<ItemDescription>();
        //    description.GlobalPosition = GetGlobalMousePosition() + new Vector2(50, 10);

        //    description.SetStats([]);
        //    description.SetItemImage(CurrentItem.FullImage);
        //    description.SetItemDescription(CurrentItem.Description);
        //    GetTree().Root.GetNode<CanvasLayer>(MainLayer).CallDeferred(Node.MethodName.AddChild, description);
        //    ItemDescription = description;
        }
    }
}
