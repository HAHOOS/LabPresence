using DiscordRPC.Logging;

using Tomlet.Attributes;

namespace LabPresence.Config
{
    public class DefaultConfig()
    {
        [TomlPrecedingComment("The logs of RPC that will be displayed, available: None, Trace, Info, Warning, Error")]
        [TomlProperty("RPCLogLevel")]
        public LogLevel RPCLogLevel { get; set; } = LogLevel.Error;

        [TomlPrecedingComment("What the Rich Presence will display as time, available options: Level (since the current level was loaded), CurrentTime (the current time, example: 15:53:50) and GameSession (since the game was launched)")]
        [TomlProperty("TimeMode")]
        public TimeMode TimeMode { get; set; } = TimeMode.GameSession;

        [TomlPrecedingComment("If true, in for example '15 - Void G114' the '15 - ' will be removed and only 'Void G114' will be shown in the 'levelName' placeholder")]
        [TomlProperty("RemoveLevelNumbers")]
        public bool RemoveLevelNumbers { get; set; } = true;

        [TomlPrecedingComment("If true, the large image will be animated (with a black blackground), otherwise a transparent static image will be used")]
        [TomlProperty("UseAnimatedLogo")]
        public bool UseAnimatedLogo { get; set; } = true;

        [TomlProperty("PreGameStarted")]
        public RpcConfig PreGameStarted { get; set; } = new("Game loading...", "{{ game.code_mods_count }} melons");

        [TomlProperty("AssetWarehouseLoaded")]
        public RpcConfig AssetWarehouseLoaded { get; set; } = new("Asset Warehouse loaded", "{{ game.mods_count }} mods");

        [TomlProperty("LevelLoaded")]
        public RpcConfig LevelLoaded { get; set; } = new("Level: {{ game.level_name }}", "Avatar: {{ player.avatar?.title | utils.clean_string }}");

        [TomlProperty("LevelLoading")]
        public RpcConfig LevelLoading { get; set; } = new("Loading {{ game.level_name }}");
    }

    public enum TimeMode
    {
        Level = 0,

        CurrentTime = 1,

        GameSession = 2
    }
}