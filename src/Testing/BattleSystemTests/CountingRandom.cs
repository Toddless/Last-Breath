namespace LastBreathTest.BattleSystemTests
{
    using System;
    using Core.Entity.Components;

    /// <summary>Counts what was asked of it: which generator a cast or its delivery actually rolled on is
    /// invisible from the outside otherwise. The stream underneath is seeded, so a drive that ends in a
    /// roll ends in the same roll every run.</summary>
    internal sealed class CountingRandom(int seed = 1) : IRandomNumberGenerator
    {
        private readonly IRandomNumberGenerator _rolls = new DefaultRandomNumberGenerator(seed);

        public int Count { get; private set; }

        public float RandFloat()
        {
            Count++;
            return _rolls.RandFloat();
        }

        public float RandFloatRange(float min, float max)
        {
            Count++;
            return _rolls.RandFloatRange(min, max);
        }

        public int RandIntRange(int min, int max)
        {
            Count++;
            return _rolls.RandIntRange(min, max);
        }

        public float RandFloatN(float mean, float deviation)
        {
            Count++;
            return _rolls.RandFloatN(mean, deviation);
        }

        public uint RandInt()
        {
            Count++;
            return _rolls.RandInt();
        }

        public long RandWeighted(float[] weights)
        {
            Count++;
            return _rolls.RandWeighted(weights);
        }

        public long RandWeighted(ReadOnlySpan<float> weights)
        {
            Count++;
            return _rolls.RandWeighted(weights);
        }

        public void Randomize() => _rolls.Randomize();
    }
}
