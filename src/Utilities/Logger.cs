namespace Utilities
{
    using Godot;
    using Serilog;

    public static class Logger
    {
        static Logger()
        {
            Configurate();
        }

        public static void LogError(string msg) => Log.Error(msg);
        public static void LogDebug(string msg) => Log.Debug(msg);
        public static void LogInfo(string msg) => Log.Information(msg);

        private static void Configurate()
        {
            var logPath = ProjectSettings.GlobalizePath("user://log.txt");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logPath)
                .CreateLogger();
        }
    }
}
