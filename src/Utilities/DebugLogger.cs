namespace Utilities
{
    using System.Runtime.CompilerServices;
    using Godot;
    using Serilog;

    public static class DebugLogger
    {
        private static ILogger s_logger;
        static DebugLogger()
        {
#if DEBUG
            var logPath = ProjectSettings.GlobalizePath("user://debugLog.txt");
            s_logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath)
                .CreateLogger();
#endif
        }

        public static void LogDebug(string msg,
           object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
#if DEBUG
            s_logger.Debug("{msg}. Source: {Source}, Method: {Method}, Line: {Line}", msg, source, method, line);
#endif
        }
    }
}
