namespace LastBreathTest.WorldTesting
{
    using System.Collections.Generic;
    using Core.World.Spaces;

    /// <summary>Explicit synthetic space membership for domain tests without native Godot objects.</summary>
    public sealed class TestSpatialQuery : ISpatialQuery
    {
        private readonly Dictionary<object, ulong> _spaces = new(ReferenceEqualityComparer.Instance);
        public ulong DefaultSpace { get; set; } = 1;
        public ulong GetSpace(object? source) => source != null && _spaces.TryGetValue(source, out var space) ? space : DefaultSpace;
        public void Set(object source, ulong space) => _spaces[source] = space;
    }
}
