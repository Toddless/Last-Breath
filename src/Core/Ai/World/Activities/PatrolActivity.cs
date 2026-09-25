namespace Core.Ai.World.Activities
{
    using System.Collections.Generic;
    using Godot;

    /// <summary>Cycles through the route points in order, pausing at each; resumes from the nearest point.</summary>
    public class PatrolActivity(IReadOnlyList<Vector2> route) : IWorldActivity
    {
        private int _index;
        private float _pauseLeft;

        public void Enter(WorldBrain brain)
        {
            if (route.Count == 0) return;
            _index = NearestPointIndex(brain.Agent.Position);
            _pauseLeft = 0;
        }

        public void Tick(WorldBrain brain, float delta)
        {
            if (route.Count == 0) return;

            var current = route[_index];
            if (brain.IsNear(current))
            {
                brain.Agent.StopMoving();
                _pauseLeft -= delta;
                if (_pauseLeft > 0) return;

                _index = (_index + 1) % route.Count;
                _pauseLeft = brain.Config.ActivityPauseSeconds;
            }

            brain.Agent.MoveTo(route[_index], brain.Config.MoveSpeed);
        }

        public void Exit(WorldBrain brain)
        {
        }

        private int NearestPointIndex(Vector2 from)
        {
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < route.Count; i++)
            {
                float distance = from.DistanceSquaredTo(route[i]);
                if (distance >= best) continue;
                best = distance;
                nearest = i;
            }

            return nearest;
        }
    }
}
