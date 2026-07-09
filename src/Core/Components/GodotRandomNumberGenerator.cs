namespace Core.Components
{
    using System;
    using Godot;

    /// <summary>Godot-backed <see cref="IRandomNumberGenerator"/>: the game runs on the engine RNG,
    /// while simulations and tests swap in <see cref="DefaultRandomNumberGenerator"/> with a fixed seed.</summary>
    public class GodotRandomNumberGenerator(RandomNumberGenerator rnd) : IRandomNumberGenerator
    {
        public float RandFloat() => rnd.Randf();

        public float RandFloatRange(float min, float max) => rnd.RandfRange(min, max);

        public int RandIntRange(int min, int max) => rnd.RandiRange(min, max);

        public float RandFloatN(float mean, float deviation) => rnd.Randfn(mean, deviation);

        public uint RandInt() => rnd.Randi();

        public long RandWeighted(float[] weights) => rnd.RandWeighted(weights);

        public long RandWeighted(ReadOnlySpan<float> weights) => rnd.RandWeighted(weights.ToArray());

        public void Randomize() => rnd.Randomize();
    }
}
