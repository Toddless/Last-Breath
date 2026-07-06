namespace Core.Components
{
    using System;
    using Interfaces.Components;

    /// <summary>
    /// Pure C# implementation of <see cref="IRandomNumberGenerator"/> (System.Random based),
    /// usable outside the Godot runtime (AI planner, unit tests). Pass a seed for determinism.
    /// </summary>
    public class DefaultRandomNumberGenerator(int? seed = null) : IRandomNumberGenerator
    {
        private Random _random = seed.HasValue ? new Random(seed.Value) : new Random();

        public float RandFloat() => (float)_random.NextDouble();

        public float RandFloatRange(float min, float max) => min + (max - min) * RandFloat();

        public int RandIntRange(int min, int max) => _random.Next(min, max + 1); // inclusive, matches Godot semantics

        public float RandFloatN(float mean, float deviation)
        {
            // Box-Muller transform
            double u1 = 1.0 - _random.NextDouble();
            double u2 = _random.NextDouble();
            double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + deviation * (float)normal;
        }

        public uint RandInt() => (uint)_random.Next();

        public long RandWeighted(float[] weights) => RandWeighted(weights.AsSpan());

        public long RandWeighted(ReadOnlySpan<float> weights)
        {
            float total = 0;
            foreach (float weight in weights) total += Math.Max(0, weight);
            if (total <= 0) return -1;

            float roll = RandFloat() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= Math.Max(0, weights[i]);
                if (roll <= 0) return i;
            }

            return weights.Length - 1;
        }

        public void Randomize() => _random = new Random();
    }
}
