namespace Utilities
{
    using System;
    using System.Runtime.CompilerServices;
    using Godot;
    using Serilog;

    public static class Logger
    {
        private static readonly ILogger s_logger;
        static Logger()
        {
            var logPath = ProjectSettings.GlobalizePath("user://log.txt");
            s_logger = new LoggerConfiguration()
                .WriteTo.File(logPath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]  {Message:lj}. Source: {Source}, Method: {Method}, Line: {Line} {NewLine}{Exception}")
                .CreateLogger();
        }

        public static void LogException(string msg,
            Exception? ex = default, object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Error(ex, msg);

        public static void LogInfo(string msg,
            object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Information(msg);
        public static void LogError(string msg,
             object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Error(msg);
        public static void LogNull(string param,
            object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Error("{param} is null.", param);
        public static void LogNotFound(string msg,
             object? source = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = 0) => ContextLogger(source, method, line).Warning("{msg} not found.", msg);

        private static ILogger ContextLogger(object? source = null,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int line = 0)
        {
            return s_logger.ForContext("Line", line)
                .ForContext("Method", method)
                .ForContext("Source", source?.GetType().Name);
        }
    }
}
