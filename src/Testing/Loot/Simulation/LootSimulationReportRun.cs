namespace LastBreathTest.Loot.Simulation
{
    /// <summary>The full Monte-Carlo balancing run: every catalog scenario, markdown report on disk.
    /// Run explicitly with: LOOT_SIMULATION_REPORT=1 dotnet test</summary>
    [TestClass]
    public class LootSimulationReportRun
    {
        private const int Seed = 20260710;
        private const int KillsPerScenario = 10_000;

        /// <summary>The opt-in this run waits for. The category alone cannot carry it: the test runner
        /// ignores VSTest's --filter, so an ordinary test run would rewrite the committed report every
        /// time and the balance it records would drift with nobody regenerating it on purpose.</summary>
        private const string OptInVariable = "LOOT_SIMULATION_REPORT";

        [TestMethod]
        [TestCategory("Simulation")]
        public async Task GenerateFullReport()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OptInVariable)))
                Assert.Inconclusive($"Set {OptInVariable}=1 to regenerate the balance report — an ordinary test run leaves the committed one alone.");

            var pipeline = LootPipeline.Create(Seed);
            var simulator = new LootSimulator(pipeline);

            var results = new List<ScenarioResult>();
            foreach (var archetype in ScenarioCatalog.All())
                results.Add(await simulator.RunAsync(archetype, KillsPerScenario));

            string markdown = ReportWriter.ToMarkdown(results, Seed);
            string reportsDirectory = Path.Combine(FindTestingRoot(), "LootSimulation", "Reports");
            Directory.CreateDirectory(reportsDirectory);
            string reportPath = Path.Combine(reportsDirectory, "LootSimulationReport.md");
            File.WriteAllText(reportPath, markdown);

            Console.WriteLine($"Report: {reportPath}");
            Console.WriteLine(markdown);
        }

        private static string FindTestingRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Data", "Shared"))) return directory.FullName;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"Data/Shared symlink not found above {AppContext.BaseDirectory}");
        }
    }
}
