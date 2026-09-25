namespace Core.Items.Use
{
    /// <summary>The one dispatch point from an item to its use behavior — the message handler
    /// executes through it, the tooltip mirrors button caption/state through it.</summary>
    public interface IItemUseService
    {
        IItemUseBehavior? BehaviorFor(IItem item);
    }
}
