namespace Core.Interfaces.Components
{
    using System;

    public interface IRandomNumberGenerator
    {
        float RandFloat();
        float RandFloatRange(float min, float max);
        float RandIntRange(int min, int max);
        float RandFloatN(float mean, float deviation);
        uint RandInt();
        long RandWeighted(float[] weights);
        long RandWeighted(ReadOnlySpan<float> weights);
        void Randomize();
    }
}
