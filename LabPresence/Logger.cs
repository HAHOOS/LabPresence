using DiscordRPC.Logging;

using MelonLoader;

namespace LabPresence
{
    public class Logger : ILogger
    {
        public LogLevel Level { get; set; }

        public string Prefix { get; internal set; }

        private MelonLogger.Instance LoggerInstance { get; }

        public Logger(MelonLogger.Instance logger, string prefix)
        {
            this.LoggerInstance = logger;
            this.Prefix = prefix;
        }

        public Logger(MelonLogger.Instance logger, LogLevel level, string prefix)
        {
            this.LoggerInstance = logger;
            this.Level = level;
            this.Prefix = prefix;
        }

        public void Error(string message, params object[] args)
        {
            if (Level > LogLevel.Error)
                return;

            LoggerInstance.Error($"[{Prefix}] [ERROR] {message}", args);
        }

        public void Info(string message, params object[] args)
        {
            if (Level > LogLevel.Info)
                return;

            LoggerInstance.Msg($"[{Prefix}] [INFO] {message}", args);
        }

        public void Trace(string message, params object[] args)
        {
            if (Level > LogLevel.Trace)
                return;

            LoggerInstance.Msg($"[{Prefix}] [TRACE] {message}", args);
        }

        public void Warning(string message, params object[] args)
        {
            if (Level > LogLevel.Warning)
                return;

            LoggerInstance.Warning($"[{Prefix}] [WARN] {message}", args);
        }
    }
}