namespace Core.World.Spaces
{
    using Godot;

    /// <summary>Resolves live space identity; zero denotes an unavailable or unknown space.</summary>
    public interface ISpatialQuery
    {
        ulong GetSpace(object? source);
        bool SharesSpace(object? source, object? target)
        {
            ulong space = GetSpace(source);
            return space != 0 && space == GetSpace(target);
        }
    }

    public sealed class NativeSpatialQuery : ISpatialQuery
    {
        public static ISpatialQuery Instance { get; } = new NativeSpatialQuery();

        public ulong GetSpace(object? source) =>
            source is Node2D node && GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion()
                ? node.GetWorld2D().GetInstanceId() : 0;
    }
}
