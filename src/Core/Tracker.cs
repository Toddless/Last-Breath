namespace Core
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Godot;
    using Serilog;

    public static class Tracker
    {
        private static readonly Lock Gate = new();
        private static ILogger? s_logger;

        static Tracker() => AssemblyUnloadCleanup.Register(CloseAndFlush);

        /// <summary>Flushes and releases the file sink; the next Track* call builds a fresh logger.
        /// Runs on editor assembly unload — an open log handle would pin the dying load context.</summary>
        public static void CloseAndFlush()
        {
            lock (Gate)
            {
                (s_logger as IDisposable)?.Dispose();
                s_logger = null;
            }
        }

        private static ILogger Logger
        {
            get
            {
                if (s_logger is { } live) return live;
                lock (Gate) return s_logger ??= CreateLogger();
            }
        }

        private static ILogger CreateLogger()
        {
            // ProjectSettings is a native engine call: outside Godot (tests, simulations) it is a fatal
            // access violation no try/catch can stop — non-Godot hosts MUST set the override first.
            string logPath = TrackerBootstrap.LogPathOverride ?? ProjectSettings.GlobalizePath("user://log.txt");
            return new LoggerConfiguration()
                .WriteTo.File(logPath,
                outputTemplate: "\n{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]  {Message:lj}. Source: {Source}, Method: {Method}, Line: {Line} {NewLine}{Exception}  \n CallStack: {CallStack}")
                .CreateLogger();
        }

        public static void TrackException(string msg,
            Exception? ex = default, object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Error(ex, msg);
        public static void TrackInfo(string msg,
            object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Information(msg);
        public static void TrackError(string msg,
             object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Error(msg);
        public static void TrackNull(string param,
            object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Error("{param} is null.", param);
        public static void TrackNotFound(string msg,
             object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Warning("{msg} not found.", msg);

        private static ILogger ContextLogger(object? source = null,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int line = 0)
        {
            string callStack = string.Join(" <= ",
                new StackTrace(2, true)
                .GetFrames()
                .Take(10));

            return Logger.ForContext("Line", line)
                .ForContext("Method", method)
                .ForContext("Source", source?.GetType().Name)
                .ForContext("CallStack", callStack);
        }
    }
}
