namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Entity;
    using Core.Enums;

    /// <summary>The flat BarrierRecovery parameter comes back at the start of the owner's turn: capped by his
    /// maximum barrier, silent when there is nothing to restore, and wired the same way on every fighter.</summary>
    [TestClass]
    public class BarrierRecoveryTests
    {
        private const float MaxBarrier = 100f;
        private const float Recovery = 10f;

        /// <summary>The fighters that recover over a turn, one file per project copy.</summary>
        private static readonly string[] s_fighterSources =
        [
            Path.Combine("Main", "Player", "Player.cs"),
            Path.Combine("Main", "Npc", "BaseNpc.cs"),
            Path.Combine("Battle", "Internal", "Player", "Player.cs"),
            Path.Combine("Battle", "Internal", "Npc", "BaseNpc.cs"),
        ];

        private static string SrcRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                    directory = directory.Parent;
                Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
                return directory.FullName;
            }
        }

        [TestMethod]
        public void TurnStart_AddsTheParameterToTheCurrentBarrier()
        {
            var fighter = Fighter(recovery: Recovery, barrier: 40f);

            TurnRecovery.ApplyTurnStart(fighter);

            Assert.AreEqual(50f, fighter.CurrentBarrier, 0.0001f);
        }

        [TestMethod]
        public void TurnStart_RestoresTheBarrierFromNothing()
        {
            var fighter = Fighter(recovery: Recovery, barrier: 0f);

            TurnRecovery.ApplyTurnStart(fighter);

            Assert.AreEqual(Recovery, fighter.CurrentBarrier, 0.0001f);
        }

        [TestMethod]
        public void TurnStart_NeverRestoresPastTheMaximumBarrier()
        {
            var fighter = Fighter(recovery: Recovery, barrier: MaxBarrier - 3f);

            TurnRecovery.ApplyTurnStart(fighter);

            Assert.AreEqual(MaxBarrier, fighter.CurrentBarrier, 0.0001f);
        }

        [TestMethod]
        public void TurnStart_WithoutTheParameter_ChangesNothingAndSignalsNothing()
        {
            var fighter = Fighter(recovery: 0f, barrier: 40f);
            int signals = 0;
            fighter.CurrentBarrierChanged += _ => signals++;

            TurnRecovery.ApplyTurnStart(fighter);

            Assert.AreEqual(40f, fighter.CurrentBarrier, 0.0001f);
            Assert.AreEqual(0, signals, "a fighter with no barrier recovery still announced a barrier change");
        }

        /// <summary>A full barrier is left alone: the aegis and the resource conditions read every barrier
        /// signal, so a restore that lands on nothing must not wake them once a turn.</summary>
        [TestMethod]
        public void TurnStart_AtAFullBarrier_SignalsNothing()
        {
            var fighter = Fighter(recovery: Recovery, barrier: MaxBarrier);
            int signals = 0;
            fighter.CurrentBarrierChanged += _ => signals++;

            TurnRecovery.ApplyTurnStart(fighter);

            Assert.AreEqual(MaxBarrier, fighter.CurrentBarrier, 0.0001f);
            Assert.AreEqual(0, signals, "a full barrier announced a change it did not make");
        }

        /// <summary>The barrier is the turn's opening, not its closing — the end of the turn pays health and
        /// mana only, or the parameter would land twice a turn.</summary>
        [TestMethod]
        public void TurnEnd_LeavesTheBarrierAlone()
        {
            var fighter = Fighter(recovery: Recovery, barrier: 40f);

            TurnRecovery.ApplyTurnEnd(fighter);

            Assert.AreEqual(40f, fighter.CurrentBarrier, 0.0001f);
        }

        /// <summary>Player and NPC, in both project copies: a copy that misses the hook keeps its barrier
        /// dead while the other side regenerates, and nothing else would say so.</summary>
        [TestMethod]
        public void EveryFighterThatRecoversOverATurnRestoresItsBarrierAtTheStart()
        {
            foreach (string relative in s_fighterSources)
            {
                string path = Path.Combine(SrcRoot, relative);
                Assert.IsTrue(File.Exists(path), $"the fighter is not where the turn hooks live: {relative}");

                string source = File.ReadAllText(path);
                StringAssert.Contains(source, "TurnRecovery.ApplyTurnStart(this);",
                    $"{relative}: the turn starts without restoring the barrier");
                StringAssert.Contains(source, "TurnRecovery.ApplyTurnEnd(this);",
                    $"{relative}: the turn ends without recovering health and mana");
            }
        }

        /// <summary>Nobody else drives the turn recovery: a fifth caller would be a second channel with its
        /// own idea of when the barrier comes back.</summary>
        [TestMethod]
        public void TheTurnRecoveryHasNoCallersBeyondTheFighters()
        {
            var expected = s_fighterSources.Select(relative => Path.Combine(SrcRoot, relative)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var callers = ShippedSources()
                .Where(path => File.ReadAllText(path).Contains("TurnRecovery.Apply", StringComparison.Ordinal))
                .Where(path => !expected.Contains(path))
                .Select(path => Path.GetRelativePath(SrcRoot, path))
                .ToList();

            Assert.AreEqual(0, callers.Count, $"the turn recovery is driven from outside the fighters: {string.Join(", ", callers)}");
        }

        /// <summary>Every source file the game ships, build output and the tests themselves aside.</summary>
        private static IEnumerable<string> ShippedSources() =>
            Directory.EnumerateFiles(SrcRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(SrcRoot, path)
                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(folder => folder is "bin" or "obj" or ".godot" or "Testing"));

        private static ConditionOwner Fighter(float recovery, float barrier)
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Barrier, MaxBarrier);
            fighter.SetMaximum(EntityParameter.BarrierRecovery, recovery);
            fighter.CurrentBarrier = barrier;
            return fighter;
        }
    }
}
