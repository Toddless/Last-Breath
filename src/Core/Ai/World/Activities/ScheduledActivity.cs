namespace Core.Ai.World.Activities
{
    using System.Collections.Generic;
    using Time;

    /// <summary>A schedule slot: minutes-of-day window (wrap-around supported) owning an activity.</summary>
    public record ScheduleSlot(int FromMinuteOfDay, int ToMinuteOfDay, IWorldActivity Activity)
    {
        /// <summary>22:00–06:00 style windows wrap through midnight.</summary>
        public bool Contains(int minuteOfDay) => FromMinuteOfDay <= ToMinuteOfDay
            ? minuteOfDay >= FromMinuteOfDay && minuteOfDay < ToMinuteOfDay
            : minuteOfDay >= FromMinuteOfDay || minuteOfDay < ToMinuteOfDay;
    }

    /// <summary>
    /// The daily routine of an NPC: delegates to whichever slot activity matches the world clock,
    /// switching with proper Exit/Enter. Not a brain change — just another activity, so alerts,
    /// chases and skirmishes interrupt schedules exactly like any Calm activity.
    /// </summary>
    public class ScheduledActivity(IWorldClock clock, IReadOnlyList<ScheduleSlot> slots, IWorldActivity fallback) : IWorldActivity
    {
        private IWorldActivity? _current;

        public void Enter(WorldBrain brain) => SwitchTo(Resolve(), brain);

        public void Tick(WorldBrain brain, float delta)
        {
            var target = Resolve();
            if (!ReferenceEquals(target, _current)) SwitchTo(target, brain);
            _current?.Tick(brain, delta);
        }

        public void Exit(WorldBrain brain)
        {
            _current?.Exit(brain);
            _current = null;
        }

        private IWorldActivity Resolve()
        {
            int now = clock.MinuteOfDay;
            foreach (var slot in slots)
                if (slot.Contains(now))
                    return slot.Activity;

            return fallback;
        }

        private void SwitchTo(IWorldActivity target, WorldBrain brain)
        {
            _current?.Exit(brain);
            _current = target;
            _current.Enter(brain);
        }
    }
}
