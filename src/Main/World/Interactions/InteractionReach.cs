namespace LastBreath.World.Interactions
{
    using System.Collections.Generic;
    using Core.World.Spaces;
    using Godot;
    using Locations;

    public static class InteractionReach
    {
        public const uint BlockerMask = 0x7fffu;

        public static float DistanceSquared(Node2D player, InteractionTarget target)
        {
            if (!GodotObject.IsInstanceValid(target) || target.IsQueuedForDeletion() || !target.IsAvailable
                || !SpatialAccess.SharesSpace(player, target) || !SpatialAccess.CanReceiveInput(target)
                || LocationRoot.Find(target) is not { IsPreparing: false }) return float.PositiveInfinity;
            var origin = Origin(player);
            float best = float.PositiveInfinity;
            foreach (var point in target.Points)
            {
                float distance = origin.DistanceSquaredTo(point);
                if (distance > target.Reach * target.Reach || distance >= best) continue;
                if (Clear(player, target.GetParent(), point, target.ObstacleMask)) best = distance;
            }
            return best;
        }

        public static bool Clear(Node2D player, Node targetOwner, Vector2 point, uint blockerMask)
        {
            var excluded = new Godot.Collections.Array<Rid>();
            foreach (var body in Bodies(player)) excluded.Add(body.GetRid());
            foreach (var body in Bodies(targetOwner)) excluded.Add(body.GetRid());
            using var query = PhysicsRayQueryParameters2D.Create(Origin(player), point, blockerMask, excluded);
            query.CollideWithAreas = false;
            query.HitFromInside = true;
            using var result = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
            return result.Count == 0;
        }

        private static Vector2 Origin(Node2D actor) => (actor as IInteractionActor)?.InteractionOrigin ?? actor.GlobalPosition;

        private static IEnumerable<CollisionObject2D> Bodies(Node node)
        {
            if (node is CollisionObject2D body) yield return body;
            for (int i = 0; i < node.GetChildCount(); i++)
                foreach (var child in Bodies(node.GetChild(i))) yield return child;
        }
    }
}
