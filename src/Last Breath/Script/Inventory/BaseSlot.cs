namespace LastBreath.Script.Inventory
{
    using Godot;
    using LastBreath.Script.Items;
    using LastBreath.Script.UI;

    public abstract partial class BaseSlot<T> : TextureButton
        where T : Item
    {
        private const string MainLayer = "Main/MainLayer";
        protected ItemDescription? ItemDescription;

        [Export] protected Texture2D? DefaltTexture;

        public T? CurrentItem { get; protected set; }


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
            if (CurrentItem == null || CurrentItem.GetItemStatsAsStrings().Count < 1) return;
            var description = ItemDescription.InitializeAsPackedScene().Instantiate<ItemDescription>();
            description.GlobalPosition = GetGlobalMousePosition() + new Vector2(50, 10);
            description.SetStats(CurrentItem.GetItemStatsAsStrings());
            description.SetItemImage(CurrentItem.FullImage);
            description.SetItemDescription(CurrentItem.Description?.Text ?? string.Empty);
            GetTree().Root.GetNode<CanvasLayer>(MainLayer).CallDeferred(Node.MethodName.AddChild, description);
            ItemDescription = description;
        }
    }
}
