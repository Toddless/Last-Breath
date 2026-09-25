namespace Battle.Internal.Tools
{
    using System.Collections.Generic;
    using Godot;

    /// <summary>
    /// Dev tool node: spawns a configured group of NPCs into the world for 1 vs N testing. Drop it into the
    /// Battle test scene, assign <c>_world</c> to MainWorld (the node battles reparent participants from),
    /// fill <c>_stats</c> in the inspector, and either let it spawn on ready or press the spawn key. The
    /// player then walks into the group to start the fight through the normal encounter flow.
    /// </summary>
    [GlobalClass]
    public partial class NpcSpawnTool : Node2D
    {
        [Export] private Node2D? _world;
        [Export] private int _count = 3;
        [Export] private float _spacing = 200f;
        [Export] private bool _spawnOnReady = true;
        [Export] private Key _spawnKey = Key.B;
        [Export] private Godot.Collections.Array<NpcStatSpec> _stats = [];

        /// <summary>Npc.json ids to spawn (cycled over count). Empty = legacy random-stat NPCs.</summary>
        [Export] private string[] _npcIds = [];

        /// <summary>Optional patrol route: a node whose Node2D children mark the waypoints in order.</summary>
        [Export] private Node2D? _patrolRoute;

        public override void _Ready()
        {
            // Deferred so the service provider / MainWorld are initialized before NPCs run their _Ready.
            if (_spawnOnReady) CallDeferred(nameof(Spawn));
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey { Pressed: true } key && key.Keycode == _spawnKey)
                Spawn();
        }

        private List<Vector2>? CollectPatrolRoute()
        {
            if (_patrolRoute == null) return null;

            List<Vector2> points = [];
            foreach (var child in _patrolRoute.GetChildren())
                if (child is Node2D marker)
                    points.Add(marker.GlobalPosition);

            return points.Count > 0 ? points : null;
        }

        private void Spawn()
        {
            var world = _world ?? GetParent() as Node2D;
            if (world == null)
            {
                GD.PrintErr("NpcSpawnTool: no world node assigned (set _world to MainWorld).");
                return;
            }

            var spawned = NpcGenerator.SpawnGroup(world, GlobalPosition, _count, [.. _stats], _spacing, _npcIds, CollectPatrolRoute());
            GD.Print($"NpcSpawnTool: spawned {spawned.Count} NPC(s) in a group.");
        }
    }
}
