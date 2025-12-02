namespace Battle.TestData
{
    using Godot;
    using System;
    using Core.Interfaces.Components;

    public class RndGodot : IRandomNumberGenerator
    {
        private readonly RandomNumberGenerator _rnd = new();

        public float RandFloat() => _rnd.Randf();
        public float RandFloatRange(float min, float max) => _rnd.RandfRange(min, max);
        public float RandIntRange(int min, int max) => _rnd.RandiRange(min, max);
        public float RandFloatN(float mean, float deviation) => _rnd.Randfn(mean, deviation);
        public uint RandInt() => _rnd.Randi();
        public long RandWeighted(float[] weights) => _rnd.RandWeighted(weights);
        public long RandWeighted(ReadOnlySpan<float> weights) => _rnd.RandWeighted(weights);
        public void Randomize() => _rnd.Randomize();
    }
}
