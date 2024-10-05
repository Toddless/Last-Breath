using Godot;
using Playground.Script.Items;

public partial class InventoryButton : Button
{
    public Item InventoryItem;
    private TextureRect _icon;
    private Label _quantityLabel;
    private int _index;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("TextureRect");
        _quantityLabel = GetNode<Label>("Label");
    }

    public void UpdateItem(Item item, int index)
    {
        this._index = index;
        InventoryItem = item;
        if (item == null)
        {
            _icon.Texture = null;
            _quantityLabel.Text = string.Empty;
        }
        else
        {
            _icon.Texture = item.Icon;
            _quantityLabel.Text = item.Quantity.ToString();
        }
    }
}
