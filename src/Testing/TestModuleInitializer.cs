namespace LastBreathTest
{
    using System.Runtime.CompilerServices;

    internal static class TestModuleInitializer
    {
        /// <summary>Runs before any test: Tracker must never call into the (absent) Godot runtime here —
        /// ProjectSettings.GlobalizePath is a fatal native access violation outside the engine.</summary>
        [ModuleInitializer]
        public static void Initialize() =>
            Core.TrackerBootstrap.LogPathOverride = Path.Combine(Path.GetTempPath(), "LastBreath", "test-log.txt");
    }
}
