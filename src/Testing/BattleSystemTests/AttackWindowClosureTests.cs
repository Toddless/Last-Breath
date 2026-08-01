namespace LastBreathTest.BattleSystemTests
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// The attack window — the stretch between the attacker announcing a hit of his and announcing it
    /// spent — is the only thing that gives a predicate about the target a subject to read. Whoever
    /// resolves the hit closes that window, and he has to close it on every way out of the resolution,
    /// a throw included: a window left open leaves the target named for everything its owner does until
    /// his next swing — someone else's damage, a heal, a parameter resolved out of combat.
    /// <para>The four fighters that resolve a hit are scene nodes no headless test can build, and their
    /// failure path prints through Godot, which is a fatal native call outside the engine — so the rule
    /// is pinned where it lives, in the source of all four copies. A copy that quietly drops it is the
    /// way the sandbox projects drift away from Main.</para>
    /// </summary>
    [TestClass]
    public class AttackWindowClosureTests
    {
        private const string Resolution = "public async Task ReceiveAttack(";
        private const string Closure = "new AfterAttackEvent(";

        /// <summary>Every copy of the hit resolution: two projects × player and NPC.</summary>
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
        public void EveryFighterSpendsHisAttackOnTheWayOutOfAThrownHit()
        {
            string[] leaking = s_fighterSources.Where(source => !SpendsOnEveryPath(source)).ToArray();

            Assert.AreEqual(0, leaking.Length,
                $"the attack is announced spent on the successful path only in {string.Join(", ", leaking)} — a throw "
                + "in the middle of the hit leaves the window open and the target readable by everything that follows");
        }

        private static bool SpendsOnEveryPath(string source)
        {
            string resolution = ResolutionBody(source);
            int[] announcements = Occurrences(resolution, Closure);

            Assert.AreEqual(1, announcements.Length,
                $"{source}: the attack has to be announced spent exactly once per hit, found {announcements.Length}");

            return IsGuaranteed(resolution, announcements[0], source);
        }

        /// <summary>The body of the hit resolution, braces matched from its signature.</summary>
        private static string ResolutionBody(string source)
        {
            string path = Path.Combine(SrcRoot, source);
            Assert.IsTrue(File.Exists(path), $"fighter source not found: {path}");

            string text = File.ReadAllText(path);
            int signature = text.IndexOf(Resolution, StringComparison.Ordinal);
            Assert.IsTrue(signature >= 0, $"{source}: no '{Resolution}' to read");

            (int start, int end) = BlockSpan(text, signature, source);

            return text[start..(end + 1)];
        }

        /// <summary>Whether the position is reached however the code around it exits: only a finally does that.</summary>
        private static bool IsGuaranteed(string code, int position, string source) =>
            Regex.Matches(code, @"\bfinally\b")
                .Select(keyword => BlockSpan(code, keyword.Index, source))
                .Any(block => position > block.Start && position < block.End);

        /// <summary>Bounds of the block opening at or after <paramref name="from"/>, closing brace included.</summary>
        private static (int Start, int End) BlockSpan(string text, int from, string source)
        {
            int open = text.IndexOf('{', from);
            Assert.IsTrue(open >= 0, $"{source}: a block was expected after position {from}");

            int depth = 0;
            for (int index = open; index < text.Length; index++)
            {
                if (text[index] == '{') depth++;
                if (text[index] == '}' && --depth == 0) return (open, index);
            }

            Assert.Fail($"{source}: the block opened at position {open} is never closed");
            return default;
        }

        private static int[] Occurrences(string code, string fragment)
        {
            var found = new List<int>();
            for (int index = code.IndexOf(fragment, StringComparison.Ordinal); index >= 0;
                 index = code.IndexOf(fragment, index + 1, StringComparison.Ordinal))
                found.Add(index);

            return [.. found];
        }
    }
}
