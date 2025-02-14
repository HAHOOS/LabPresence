using Tomlet.Attributes;

namespace LabPresence.Config
{
    public class DefaultConfig()
    {
        [TomlProperty("RefreshDelay")]
        public float RefreshDelay { get; set; } = 0.75f;

        [TomlProperty("PreGameStarted")]
        public RPCConfig PreGameStarted { get; set; } = new("Game loading...", "%codeModsCount% melons");

        [TomlProperty("MarrowGameStarted")]
        public RPCConfig MarrowGameStarted { get; set; } = new("Game started");

        [TomlProperty("AssetWarehouseLoaded")]
        public RPCConfig AssetWarehouseLoaded { get; set; } = new("Asset Warehouse loaded", "%modsCount% mods");

        [TomlProperty("LevelLoaded")]
        public RPCConfig LevelLoaded { get; set; } = new("Level: %levelName%", "Avatar: %avatarName%");

        [TomlProperty("LevelLoading")]
        public RPCConfig LevelLoading { get; set; } = new("Loading %levelName%");
    }
}