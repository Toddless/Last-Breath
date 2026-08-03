namespace LastBreathTest.BattleSystemTests
{
    using System;
    using Battle.Source.Abilities;
    using Core.Entity.Components;

    /// <summary>
    /// Puts a pure-C# generator in the seat a cast rolls on for as long as the scope lives and hands the
    /// game's engine source back at the end. Nothing outside Godot may build the engine generator — it is
    /// a native object and constructing one takes the whole test host down (0xC0000005) — and handing the
    /// source back is what keeps the pin on the production choice readable to whoever runs next.
    /// </summary>
    internal sealed class CastRandomScope : IDisposable
    {
        public CastRandomScope(IRandomNumberGenerator generator)
        {
            Generator = generator;
            Ability.CastRandomSource = () => generator;
        }

        public IRandomNumberGenerator Generator { get; }

        /// <summary>A reproducible stream: the same seed rolls the same casts run after run.</summary>
        public static CastRandomScope Seeded(int seed = 1) => new(new DefaultRandomNumberGenerator(seed));

        public void Dispose() => Ability.CastRandomSource = Ability.EngineCastRandom;
    }
}
