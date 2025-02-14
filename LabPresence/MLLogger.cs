using DiscordRPC.Logging;

using MelonLoader;

namespace LabPresence
{
    internal class MLLogger : ILogger
    {
        public LogLevel Level { get; set; }

        private MelonLogger.Instance Logger { get; }

        public MLLogger(MelonLogger.Instance logger)
        {
            this.Logger = logger;
        }

        public MLLogger(MelonLogger.Instance logger, LogLevel level)
        {
            this.Logger = logger;
            this.Level = level;
        }

        public void Error(string message, params object[] args)
        {
            if (Level > LogLevel.Error)
                return;

            Logger.Error($"[RPC] [ERROR] {message}", args);
        }

        public void Info(string message, params object[] args)
        {
            if (Level > LogLevel.Info)
                return;

            Logger.Msg($"[RPC] [INFO] {message}", args);
        }

        public void Trace(string message, params object[] args)
        {
            if (Level > LogLevel.Trace)
                return;

            Logger.Msg($"[RPC] [TRACE] {message}", args);
        }

        public void Warning(string message, params object[] args)
        {
            if (Level > LogLevel.Warning)
                return;

            Logger.Warning($"[RPC] [WARN] {message}", args);
        }
    }
}