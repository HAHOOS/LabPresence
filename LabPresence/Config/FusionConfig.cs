using Tomlet.Attributes;

namespace LabPresence.Config
{
    /// <summary>
    /// The config that contains the settings for LabFusion
    /// </summary>
    public class FusionConfig()
    {
        /// <summary>
        /// If the time mode will be set to 'Level', when in a fusion lobby it will override the time to display how long you are in the lobby instead of the level
        /// </summary>
        [TomlPrecedingComment("If the time mode will be set to 'Level', when in a fusion lobby it will override the time to display how long you are in the lobby instead of the level")]
        [TomlProperty("OverrideTimeToLobby")]
        public bool OverrrideTimeToLobby { get; set; } = true;

        /// <summary>
        /// If true, a notification will be shown when someone requests to join your server
        /// </summary>
        [TomlPrecedingComment("If true, a notification will be shown when someone requests to join your server")]
        [TomlProperty("ShowJoinRequestPopUp")]
        public bool ShowJoinRequestPopUp { get; set; } = true;

        /// <summary>
        /// If true, when hosting a friends only / private server, players will be able to let others join the server through Discord
        /// </summary>
        [TomlPrecedingComment("If true, when hosting a friends only / private server, players will be able to let others join the server through Discord")]
        [TomlProperty("AllowPlayersToInvite")]
        public bool AllowPlayersToInvite { get; set; } = true;

        /// <summary>
        /// If true, gamemodes that support custom tooltips will display custom text on the small icon. Disabling this option will cause the tooltip to only show the name of the gamemode
        /// </summary>
        [TomlPrecedingComment("If true, gamemodes that support custom tooltips will display custom text on the small icon. Disabling this option will cause the tooltip to only show the name of the gamemode")]
        [TomlProperty("ShowCustomGamemodeToolTips")]
        public bool ShowCustomGamemodeToolTips { get; set; } = true;

        /// <summary>
        /// Config used when a level gets loaded and you are in a lobby
        /// </summary>
        [TomlProperty("LevelLoaded")]
        public RPCConfig LevelLoaded { get; set; } = new("%levelName%", "%fusion_lobbyName%");

        /// <summary>
        /// Config used when a level is loading and you are in a lobby
        /// </summary>
        [TomlProperty("LevelLoading")]
        public RPCConfig LevelLoading { get; set; } = new("Loading %levelName%", "%fusion_lobbyName%");
    }
}