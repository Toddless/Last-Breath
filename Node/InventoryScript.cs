using Godot;
using Playground.Script.Items;
using System.Collections.Generic;
using System.Linq;

public partial class InventoryScript : Control
{
    private PackedScene _inventoryButton;
    [Export]
    private string _itemButtonPath = "res://UI/InventoryButton.tscn";
    [Export]
    public int Capacity { get; set; } = 31;
    public InventoryButton GrabbedObject { get; set; }
    public InventoryButton HoverOverButton { get; set; }
    private Vector2 _lastMoiseClickPosition;
    private List<Item> _items = [];
    private GridContainer _gridContainer;
    private bool _isOverTrash;

    public override void _Ready()
    {
        _gridContainer = GetNode<GridContainer>("GridContainer");
        _inventoryButton = ResourceLoader.Load<PackedScene>(_itemButtonPath);
        PopulateButtons();
    }

    public override void _Process(double delta)
    {
        GetNode<Area2D>("MouseArea2D").Position = GetTree().Root.GetMousePosition();
        if (HoverOverButton != null)
        {
            if (Input.IsActionJustPressed("LMB"))
            {
                GrabbedObject = HoverOverButton;
                _lastMoiseClickPosition = GetTree().Root.GetMousePosition();
            }

            if (_lastMoiseClickPosition.DistanceTo(GetTree().Root.GetMousePosition()) > 2)
            {
                if (Input.IsActionPressed("LMB"))
                {
                    InventoryButton butt = GetNode<Area2D>("MouseArea2D").GetNode<InventoryButton>("InventoryButton");
                    butt.Show();
                    butt.UpdateItem(GrabbedObject.InventoryItem, 0);
                }
                if (Input.IsActionJustReleased("LMB"))
                {
                    if (_isOverTrash)
                    {
                        DeleteButton(GrabbedObject);
                    }
                    else
                    {
                        SwapInventoryItems(GrabbedObject, HoverOverButton);
                        InventoryButton butt = GetNode<Area2D>("MouseArea2D").GetNode<InventoryButton>("InventoryButton");
                        butt.Hide();
                    }
                }
            }
        }
        if (Input.IsActionJustReleased("LMB") && _isOverTrash)
        {
            DeleteButton(GrabbedObject);
        }
    }

    public void DeleteButton(InventoryButton button)
    {
        _items.Remove(button.InventoryItem);
        RefreshButtons();
        InventoryButton butt = GetNode<Area2D>("MouseArea2D").GetNode<InventoryButton>("InventoryButton");
        butt.Hide();
    }

    public void Add(Item item)
    {
        Item currentItem = item.Copy();
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Guid == currentItem.Guid && _items[i].Quantity != _items[i].StackSize)
            {
                if (_items[i].Quantity + currentItem.Quantity > _items[i].StackSize)
                {
                    _items[i].Quantity = currentItem.StackSize;
                    currentItem.Quantity = _items[i].StackSize - currentItem.Quantity;
                    UpdateButton(i);
                }
                else
                {
                    _items[i].Quantity += currentItem.Quantity;
                    currentItem.Quantity = 0;
                    UpdateButton(i);
                }
            }
        }

        if (currentItem.Quantity > 0)
        {
            if (currentItem.Quantity < currentItem.StackSize)
            {
                _items.Add(currentItem);
                UpdateButton(_items.Count - 1);

            }
            else
            {
                // при добавлении 15 элементов с максимальным стак сайзом 2
                // при повторном нажатии кнопки Add происходит добавление одного элемента в последний стак
                // оставшиеся 14 элементов пропадают
                Item tempItem = currentItem.Copy();
                tempItem.Quantity = currentItem.StackSize;
                _items.Add(tempItem);
                UpdateButton(_items.Count - 1);
                currentItem.Quantity -= currentItem.StackSize;
                Add(currentItem);
            }
        }
    }

    public void UpdateButton(int index)
    {
        if (_items.ElementAtOrDefault(index) != null)
        {
            _gridContainer.GetChild<InventoryButton>(index).UpdateItem(_items[index], index);
        }
        else
        {
            _gridContainer.GetChild<InventoryButton>(index).UpdateItem(null, index);
        }
    }

    public void OnRemoveButtonDown()
    {
        Remove(ResourceLoader.Load<Item>("res://Item.tres"));
    }

    public void OnAddButtonDown()
    {
        Add(ResourceLoader.Load<Item>("res://Item.tres"));
    }

    public void OnMouseArea2dEntered(Area2D area)
    {
        Control butt = area.GetParent<Control>();
        if (butt is InventoryButton button)
        {
            HoverOverButton = button;
        }
    }

    public void OnMouseArea2dExited(Area2D area) => HoverOverButton = null;

    public void OnTrashArea2dEntered(Area2D area) => _isOverTrash = true;

    public void OnTrashArea2dExited(Area2D area) => _isOverTrash = false;

    private void SwapInventoryItems(InventoryButton butt1, InventoryButton butt2)
    {
        int buttIndex = butt1.GetIndex();
        int butt2Index = butt2.GetIndex();
        _gridContainer.MoveChild(butt1, butt2Index);
        _gridContainer.MoveChild(butt2, buttIndex);
    }

    private void PopulateButtons()
    {
        for (int i = 0; i < Capacity; i++)
        {
            InventoryButton currentInventoryButton = _inventoryButton.Instantiate<InventoryButton>();
            _gridContainer.AddChild(currentInventoryButton);
        }
    }

    private bool Remove(Item item)
    {
        if (!CanAfford(item))
        {
            return false;
        }
        Item currentItem = item.Copy();
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Guid == currentItem.Guid)
            {
                if (_items[i].Quantity - currentItem.Quantity < 0)
                {
                    currentItem.Quantity -= _items[i].Quantity;
                    _items[i].Quantity = 0;
                    UpdateButton(i);
                }
                else
                {
                    _items[i].Quantity -= currentItem.Quantity;
                    currentItem.Quantity = 0;
                    UpdateButton(i);
                }
            }

            if (currentItem.Quantity <= 0)
            {
                break;
            }
        }

        _items.RemoveAll(x => x.Quantity <= 0);
        if (currentItem.Quantity > 0)
        {
            Remove(currentItem);
        }
        RefreshButtons();
        return true;

    }

    private bool CanAfford(Item item)
    {
        List<Item> currentItems = _items.Where(x => x.Guid == item.Guid).ToList();
        int i = 0;
        foreach (var item1 in currentItems)
        {
            i += item1.Quantity;
        }
        if (item.Quantity < i)
        {
            return true;
        }
        return false;
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < Capacity; i++)
        {
            UpdateButton(i);
        }
    }
}
