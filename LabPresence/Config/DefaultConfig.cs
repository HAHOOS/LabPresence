using DiscordRPC.Logging;

using Tomlet.Attributes;

namespace LabPresence.Config
{
    /// <summary>
    /// The config that contains the settings for the core mod, not the support for stuff like Fusion
    /// </summary>
    public class DefaultConfig()
    {
        /// <summary>
        /// The delay at which the Rich Presence will update.
        ///
        /// <para>Note that using some placeholders will have a minimum delay that if higher than the set one, will override when the placeholder is present on the Rich Presence</para>
        /// </summary>
        [TomlPrecedingComment("The delay at which the Rich Presence will update.\nNote that using some placeholders will have a minimum delay that if higher than the set one, will override when the placeholder is present on the Rich Presence")]
        [TomlProperty("RefreshDelay")]
        public float RefreshDelay { get; set; } = 0.75f;

        /// <summary>
        /// The logs of RPC that will be displayed, available: None, Trace, Info, Warning, Error
        /// </summary>
        [TomlPrecedingComment("The logs of RPC that will be displayed, available: None, Trace, Info, Warning, Error")]
        [TomlProperty("RPCLogLevel")]
        public LogLevel RPCLogLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// What the RPC will display as time, available options: Level (since the current level was loaded), CurrentTime (the current time, example: 15:53:50) and GameSession (since the game was launched)
        /// </summary>
        [TomlPrecedingComment("What the RPC will display as time, available options: Level (since the current level was loaded), CurrentTime (the current time, example: 15:53:50) and GameSession (since the game was launched)")]
        [TomlProperty("TimeMode")]
        public TimeModeEnum TimeMode { get; set; } = TimeModeEnum.GameSession;

        /// <summary>
        /// If true, in for example '15 - Void G114' the '15 - ' will be removed and only 'Void G114' will be shown in the %levelName% placeholder
        /// </summary>
        [TomlPrecedingComment("If true, in for example '15 - Void G114' the '15 - ' will be removed and only 'Void G114' will be shown in the %levelName% placeholder")]
        [TomlProperty("RemoveLevelNumbers")]
        public bool RemoveLevelNumbers { get; set; } = true;

        /// <summary>
        /// The config used before the game starts
        /// </summary>
        [TomlProperty("PreGameStarted")]
        public RPCConfig PreGameStarted { get; set; } = new("Game loading...", "%codeModsCount% melons");

        /// <summary>
        /// The config used when all the mods are loaded
        /// </summary>
        [TomlProperty("AssetWarehouseLoaded")]
        public RPCConfig AssetWarehouseLoaded { get; set; } = new("Asset Warehouse loaded", "%modsCount% mods");

        /// <summary>
        /// The config used when the a level is loaded
        /// </summary>
        [TomlProperty("LevelLoaded")]
        public RPCConfig LevelLoaded { get; set; } = new("Level: %levelName%", "Avatar: %avatarName%");

        /// <summary>
        /// The config used when the a level is loading
        /// </summary>
        [TomlProperty("LevelLoading")]
        public RPCConfig LevelLoading { get; set; } = new("Loading %levelName%");

        /// <summary>
        /// The options for <see cref="TimeMode"/>
        /// </summary>
        public enum TimeModeEnum
        {
            /// <summary>
            /// Since the current level was loaded
            /// </summary>
            Level,

            /// <summary>
            /// The current time, example: 15:53:50
            /// </summary>
            CurrentTime,

            /// <summary>
            /// Since the game was launched
            /// </summary>
            GameSession
        }
    }
}