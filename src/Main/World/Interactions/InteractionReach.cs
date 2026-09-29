namespace LastBreath.World.Interactions
{
    using System.Collections.Generic;
    using Core.World.Spaces;
    using Godot;
    using Locations;

    public static class InteractionReach
    {
        /// <summary>Ray query every reach check reuses: physics queries run on the main thread only, so the shared query and lists need no guard.</summary>
        private static readonly PhysicsRayQueryParameters2D s_query = new() { CollideWithAreas = false, HitFromInside = true };

        /// <summary>Bodies the query excludes; the query copies this list whenever it is handed over, so that happens only when the bodies change.</summary>
        private static readonly Godot.Collections.Array<Rid> s_excluded = [];

        /// <summary>Bodies the current ray ignores: the player's, then the target's that are still valid.</summary>
        private static readonly List<Rid> s_ignored = [];

        public const uint BlockerMask = 0x7fffu;

        public static float DistanceSquared(Node2D player, InteractionTarget target)
        {
            if (!GodotObject.IsInstanceValid(target) || target.IsQueuedForDeletion() || !target.IsAvailable
                || !SpatialAccess.SharesSpace(player, target) || !SpatialAccess.CanReceiveInput(target)
                || LocationRoot.Find(target) is not { IsPreparing: false }) return float.PositiveInfinity;
            var origin = Origin(player);
            float best = float.PositiveInfinity;
            foreach (var point in target.ReadPoints())
            {
                float distance = origin.DistanceSquaredTo(point);
                if (distance > target.Reach * target.Reach || distance >= best) continue;
                if (Clear(player, target.OwnBodies, point, target.ObstacleMask)) best = distance;
            }
            return best;
        }

        /// <summary>True when no blocker crosses the segment from the player to the point; the player's bodies and the live target bodies are ignored.</summary>
        public static bool Clear(Node2D player, IReadOnlyList<CollisionObject2D> targetBodies, Vector2 point, uint blockerMask)
        {
            CollectIgnored(player, targetBodies);
            ExcludeIgnored();
            s_query.From = Origin(player);
            s_query.To = point;
            s_query.CollisionMask = blockerMask;
            using var result = player.GetWorld2D().DirectSpaceState.IntersectRay(s_query);
            return result.Count == 0;
        }

        private static Vector2 Origin(Node2D actor) => (actor as IInteractionActor)?.InteractionOrigin ?? actor.GlobalPosition;

        /// <summary>Collects the bodies the ray ignores: the player's, then the target's that are still valid.</summary>
        private static void CollectIgnored(Node2D player, IReadOnlyList<CollisionObject2D> targetBodies)
        {
            s_ignored.Clear();
            AddBodies(player);
            for (int i = 0; i < targetBodies.Count; i++)
            {
                var body = targetBodies[i];
                if (GodotObject.IsInstanceValid(body)) s_ignored.Add(body.GetRid());
            }
        }

        /// <summary>Adds the node when it is a collision object, then every collision object below it, in tree order.</summary>
        private static void AddBodies(Node node)
        {
            if (node is CollisionObject2D body) s_ignored.Add(body.GetRid());
            for (int i = 0; i < node.GetChildCount(); i++) AddBodies(node.GetChild(i));
        }

        /// <summary>Hands the collected bodies to the query as its exclusion, unless it already excludes exactly them.</summary>
        private static void ExcludeIgnored()
        {
            if (ExcludesIgnored()) return;
            s_excluded.Clear();
            for (int i = 0; i < s_ignored.Count; i++) s_excluded.Add(s_ignored[i]);
            s_query.Exclude = s_excluded;
        }

        /// <summary>True when the query already excludes the collected bodies, in the same order.</summary>
        private static bool ExcludesIgnored()
        {
            if (s_excluded.Count != s_ignored.Count) return false;
            for (int i = 0; i < s_ignored.Count; i++)
                if (s_excluded[i] != s_ignored[i]) return false;
            return true;
        }
    }
}
