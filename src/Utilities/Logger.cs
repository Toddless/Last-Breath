namespace Utilities
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Godot;
    using Serilog;

    public static class Logger
    {
        static Logger()
        {
            Configurate();
        }

        public static void LogException(string msg,
            Exception? ex = default, object? source = null, [CallerMemberName] string method = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, file, line).Error(ex, msg);
        public static void LogDebug(string msg,
            object? source = null, [CallerMemberName] string method = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, file, line).Debug(msg);
        public static void LogInfo(string msg,
            object? source = null, [CallerMemberName] string method = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, file, line).Information(msg);
        public static void LogError(string msg,
             object? source = null, [CallerMemberName] string method = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, file, line).Error(msg);
        public static void LogNull(string param,
            object? source = null, [CallerMemberName] string method = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, file, line).Error("{param} is null.", param);
        public static void LogNotFound(string msg,
             object? source = null, [CallerMemberName] string method = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, file, line).Warning("{msg} not found.", msg);

        private static ILogger ContextLogger(object? source = null,
            [CallerMemberName] string method = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            return Log.ForContext("Source", source?.GetType().Name)
                    .ForContext("Method", method)
                    .ForContext("File", Path.GetFileName(file))
                    .ForContext("Line", line);
        }

        private static void Configurate()
        {
            var logPath = ProjectSettings.GlobalizePath("user://log.txt");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logPath)
                .CreateLogger();
        }
    }
}
