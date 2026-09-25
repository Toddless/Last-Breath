namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.IO;
    using Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>The editor unload cleanup closes the Tracker's file sink; life after a reload depends
    /// on the logger coming back lazily and the handle actually being released.</summary>
    [TestClass]
    public sealed class TrackerCleanupTests
    {
        [TestMethod]
        public void CloseAndFlush_ReleasesHandleAndNextCallLogsAgain()
        {
            string? original = TrackerBootstrap.LogPathOverride;
            string path = Path.Combine(Path.GetTempPath(), "LastBreath", $"tracker-cleanup-{Guid.NewGuid():N}.txt");
            try
            {
                Tracker.CloseAndFlush(); // detach from the run's shared log so the override below is picked up
                TrackerBootstrap.LogPathOverride = path;

                Tracker.TrackInfo("before close");
                Tracker.CloseAndFlush();
                Assert.IsTrue(ReadExclusively(path).Contains("before close", StringComparison.Ordinal),
                    "The message logged before the close must be flushed to the file.");

                Tracker.TrackInfo("after close");
                Tracker.CloseAndFlush();
                Assert.IsTrue(ReadExclusively(path).Contains("after close", StringComparison.Ordinal),
                    "A Track* call after CloseAndFlush must rebuild the logger and write again.");
            }
            finally
            {
                Tracker.CloseAndFlush();
                TrackerBootstrap.LogPathOverride = original;
                TryDelete(path);
            }
        }

        [TestMethod]
        public void CloseAndFlush_WithoutLoggerIsHarmless()
        {
            Tracker.CloseAndFlush();
            Tracker.CloseAndFlush(); // a second unload pass finds nothing to release and stays quiet
        }

        /// <summary>Reads with an exclusive share: succeeds only when the sink's handle is gone.</summary>
        private static string ReadExclusively(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // A straggling handle only means the temp file outlives the run; the assertion above already failed loudly.
            }
        }
    }
}
