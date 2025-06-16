using DiscordRPC.Logging;

using MelonLoader;

namespace LabPresence
{
    /// <summary>
    /// Class used for the Rich Presence Logger and <see cref="Plugins.Plugin"/>
    /// </summary>
    public class Logger : ILogger
    {
        /// <summary>
        /// The minimum level that a message needs to have to be sent
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// The prefix for the message
        /// <para>
        /// Example: [your prefix here] Lorem ipsum dolor sit amet.
        /// </para>
        /// </summary>
        public string Prefix { get; internal set; }

        /// <summary>
        /// The logger to be used to send the actual message
        /// </summary>
        private MelonLogger.Instance LoggerInstance { get; }

        /// <summary>
        /// Initialize a new instance of <see cref="Logger"/>
        /// </summary>
        /// <param name="logger"><inheritdoc cref="LoggerInstance"/></param>
        /// <param name="prefix"><inheritdoc cref="Prefix"/></param>
        public Logger(MelonLogger.Instance logger, string prefix)
        {
            this.LoggerInstance = logger;
            this.Prefix = prefix;
        }

        /// <summary>
        /// Initialize a new instance of <see cref="Logger"/>
        /// </summary>
        /// <param name="logger"><inheritdoc cref="LoggerInstance"/></param>
        /// <param name="level"><inheritdoc cref="Level"/></param>
        /// <param name="prefix"><inheritdoc cref="Prefix"/></param>
        public Logger(MelonLogger.Instance logger, LogLevel level, string prefix)
        {
            this.LoggerInstance = logger;
            this.Level = level;
            this.Prefix = prefix;
        }

        /// <inheritdoc cref="ILogger.Error(string, object[])"/>
        public void Error(string message, params object[] args)
        {
            if (Level > LogLevel.Error)
                return;

            LoggerInstance.Error($"[{Prefix}] [ERROR] {message}", args);
        }

        /// <inheritdoc cref="ILogger.Info(string, object[])"/>
        public void Info(string message, params object[] args)
        {
            if (Level > LogLevel.Info)
                return;

            LoggerInstance.Msg($"[{Prefix}] [INFO] {message}", args);
        }

        /// <inheritdoc cref="ILogger.Trace(string, object[])"/>
        public void Trace(string message, params object[] args)
        {
            if (Level > LogLevel.Trace)
                return;

            LoggerInstance.Msg($"[{Prefix}] [TRACE] {message}", args);
        }

        /// <inheritdoc cref="ILogger.Warning(string, object[])"/>
        public void Warning(string message, params object[] args)
        {
            if (Level > LogLevel.Warning)
                return;

            LoggerInstance.Warning($"[{Prefix}] [WARN] {message}", args);
        }
    }
}