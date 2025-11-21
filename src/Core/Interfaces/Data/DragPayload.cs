namespace Core.Interfaces.Data
{
    using Items;

    public class DragPayload(IItem item, int quantity, object source)
    {
        public IItem Item = item;
        public int Quantity = quantity;
        public object Source = source;
    }
}
