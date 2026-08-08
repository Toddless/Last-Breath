namespace Core.Ai.World
{
    using Entity.Components;

    /// <summary>
    /// The lying countdown shared by the post-defeat cycles: its length is rolled once when the body
    /// goes down, then ticked seconds are counted until it is spent. Knows nothing about stages or
    /// endings — what going down and getting up mean belongs to the cycle that owns the timer.
    /// </summary>
    public class BodyRiseTimer(IRandomNumberGenerator rnd)
    {
        /// <summary>The rolled length of the current countdown.</summary>
        public float Delay { get; private set; }

        /// <summary>Seconds already counted of the current countdown.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Rolls a fresh length inside the range and starts counting from zero.</summary>
        public void Roll(float minSeconds, float maxSeconds)
        {
            Elapsed = 0;
            Delay = rnd.RandFloatRange(minSeconds, maxSeconds);
        }

        /// <summary>Save-load path: resumes a countdown that was already running.</summary>
        public void Restore(float delay, float elapsed)
        {
            Delay = delay;
            Elapsed = elapsed;
        }

        /// <summary>Adds the ticked seconds; true once the countdown is spent.</summary>
        public bool Advance(float delta)
        {
            Elapsed += delta;
            return Elapsed >= Delay;
        }
    }
}
