namespace LastBreathTest.BattleSystemTests
{
    using System;
    using Battle.Source;
    using Core.Entity.Components;

    /// <summary>
    /// Puts a generator of the test's choosing — a seeded stream, a counting spy — in the seat ability
    /// delivery rolls on for as long as the scope lives, and hands the seat back to the domain default at
    /// the end. The attack face stays empty on purpose: it is the engine object, nothing outside Godot may
    /// build one, and nothing in a sandbox resolves an attack far enough to read it.
    /// </summary>
    internal sealed class CombatRandomScope : IDisposable
    {
        public CombatRandomScope(IRandomNumberGenerator rolls)
        {
            Rolls = rolls;
            CombatRandom.Source = () => new CombatRandomStream(rolls, Attacks: null);
        }

        public IRandomNumberGenerator Rolls { get; }

        public void Dispose() => CombatRandom.Source = CombatRandom.DefaultCombatRandom;
    }
}
