namespace LastBreathTest.LootSimulation
{
    /// <summary>The full Monte-Carlo balancing run: every catalog scenario, markdown report on disk.
    /// Run explicitly with: dotnet test --filter "TestCategory=Simulation"</summary>
    [TestClass]
    public class LootSimulationReportRun
    {
        private const int Seed = 20260710;
        private const int KillsPerScenario = 10_000;

        [TestMethod]
        [TestCategory("Simulation")]
        public async Task GenerateFullReport()
        {
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
