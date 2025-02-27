using DiscordRPC.Logging;

using Tomlet.Attributes;

namespace LabPresence.Config
{
    public class DefaultConfig()
    {
        [TomlPrecedingComment("The delay at which the Rich Presence will update.\nNote that using some placeholders will have a minimum delay that if higher than the set one, will override when the placeholder is present on the Rich Presence")]
        [TomlProperty("RefreshDelay")]
        public float RefreshDelay { get; set; } = 0.75f;

        [TomlPrecedingComment("The logs of RPC that will be displayed, available: None, Trace, Info, Warning, Error")]
        [TomlProperty("RPCLogLevel")]
        public LogLevel RPCLogLevel { get; set; } = LogLevel.Error;

        [TomlPrecedingComment("What the RPC will display as time, available options: Level (since the current level was loaded), CurrentTime (the current time, example: 15:53:50) and GameSession (since the game was launched)")]
        [TomlProperty("TimeMode")]
        public TimeModeEnum TimeMode { get; set; } = TimeModeEnum.GameSession;

        [TomlPrecedingComment("If true, in for example '15 - Void G114' the '15 - ' will be removed and only 'Void G114' will be shown in the %levelName% placeholder")]
        [TomlProperty("RemoveLevelNumbers")]
        public bool RemoveLevelNumbers { get; set; } = true;

        [TomlProperty("PreGameStarted")]
        public RPCConfig PreGameStarted { get; set; } = new("Game loading...", "%codeModsCount% melons");

        [TomlProperty("AssetWarehouseLoaded")]
        public RPCConfig AssetWarehouseLoaded { get; set; } = new("Asset Warehouse loaded", "%modsCount% mods");

        [TomlProperty("LevelLoaded")]
        public RPCConfig LevelLoaded { get; set; } = new("Level: %levelName%", "Avatar: %avatarName%");

        [TomlProperty("LevelLoading")]
        public RPCConfig LevelLoading { get; set; } = new("Loading %levelName%");

        public enum TimeModeEnum
        {
            Level,
            CurrentTime,
            GameSession
        }
    }
}