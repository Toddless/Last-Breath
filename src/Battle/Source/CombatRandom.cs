namespace Battle.Source
{
    using System;
    using Core.Entity.Components;
    using Godot;

    /// <summary>
    /// The two faces of one combat stream. Rolls is what delivery asks for its own decisions — how long
    /// a series runs, which enemy a bounce lands on, whether a retaliation shaves a cooldown. Attacks is
    /// the engine object itself, carried by every attack context because
    /// <see cref="Core.Battle.IAttackContext.Rnd"/> names that class and nothing else fits; it is absent
    /// wherever no engine is seated, and so is anything that resolves an attack for real.
    /// </summary>
    public sealed record CombatRandomStream(IRandomNumberGenerator Rolls, RandomNumberGenerator? Attacks);

    /// <summary>
    /// The stream combat rolls on once a cast is past its activation gate — the same seat idea as
    /// <see cref="Abilities.Ability.CastRandomSource"/>, one step further down the cast. It starts on
    /// <see cref="DefaultCombatRandom"/>, which touches nothing native: the engine generator is a native
    /// object, and constructing one where Godot is not running kills the whole process (0xC0000005) past
    /// the reach of any catch. Which implementation the running game rolls on is a composition decision —
    /// the bootstrap puts <see cref="EngineCombatRandom"/> here, so delivery rolls on the engine RNG like
    /// the rest of combat. A new source also drops the stream currently in use, so the swap holds however
    /// much has already been delivered.
    /// </summary>
    public static class CombatRandom
    {
        private static CombatRandomStream? s_stream;

        public static Func<CombatRandomStream> Source
        {
            get;
            set
            {
                field = value;
                s_stream = null;
            }
        } = DefaultCombatRandom;

        /// <summary>What delivery rolls its own decisions on.</summary>
        public static IRandomNumberGenerator Rolls => Stream.Rolls;

        /// <summary>The generator an attack context carries — null where the composition seated no
        /// engine stream, which is also where no attack is ever resolved far enough to read it.</summary>
        public static RandomNumberGenerator? Attacks => Stream.Attacks;

        /// <summary>The stream the running game delivers on: the engine RNG, time-seeded, worn as both
        /// faces so a cast rolls everything off one sequence.</summary>
        public static CombatRandomStream EngineCombatRandom()
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            return new CombatRandomStream(new GodotRandomNumberGenerator(rnd), rnd);
        }

        /// <summary>The stream delivery rolls on until a composition says otherwise: pure C#, time-seeded
        /// and free of the engine, so a host that never boots Godot survives a delivery instead of dying
        /// on the first one.</summary>
        public static CombatRandomStream DefaultCombatRandom() => new(new DefaultRandomNumberGenerator(), Attacks: null);

        private static CombatRandomStream Stream => s_stream ??= Source();
    }
}
