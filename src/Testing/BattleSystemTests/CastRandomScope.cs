namespace LastBreathTest.BattleSystemTests
{
    using System;
    using Battle.Source.Abilities;
    using Core.Entity.Components;

    /// <summary>
    /// Puts a generator of the test's choosing — a seeded stream, a counting spy — in the seat a cast
    /// rolls on for as long as the scope lives, and hands the seat back to the domain default at the end.
    /// The default is what the engine source is NOT: nothing outside Godot may build the engine
    /// generator, so leaving it behind here would arm the next cast of whoever runs next.
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

        public void Dispose() => Ability.CastRandomSource = Ability.DefaultCastRandom;
    }
}
