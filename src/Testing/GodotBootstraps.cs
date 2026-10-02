namespace LastBreathTest
{
    using System;

    /// <summary>
    /// The projects that boot inside Godot, and therefore owe the running game every seat that must hold
    /// the engine generator. They are the Godot projects, off the test assembly's references, so the fact
    /// that they still install a hook is read off their source.
    /// </summary>
    internal static class GodotBootstraps
    {
        private static readonly string[] s_bootstraps =
        [
            Path.Combine("Main", "Services", "GameServiceProvider.cs"),
            Path.Combine("Battle", "Services", "GameServiceProvider.cs"),
        ];

        /// <summary>Fails unless every Godot bootstrap still calls the named composition hook. A project
        /// that drops the call leaves that seat on the engine-free default, and nothing in the running
        /// game would ever say so out loud.</summary>
        public static void AssertEachInstalls(string hook)
        {
            foreach (string relative in s_bootstraps)
                AssertInstalls(relative, hook);
        }

        /// <summary>Fails unless the named source still fills a seat with the named hook. For seats that
        /// belong to one project only — a sandbox scene wiring its own collaborator — there is nothing to
        /// walk, so the source is named outright.</summary>
        public static void AssertInstalls(string relativeSource, string hook)
        {
            string source = Path.Combine(SrcRoot, relativeSource);
            Assert.IsTrue(File.Exists(source), $"the composition is not where it lived: {source}");
            Assert.IsTrue(
                File.ReadAllText(source).Contains(hook, StringComparison.Ordinal),
                $"{relativeSource} stopped calling {hook} — the game would roll on the sandbox default");
        }

        /// <summary>The sources the game ships, found by walking up from the test binaries.</summary>
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
    }
}
