using Tomlet.Attributes;

namespace LabPresence.Config
{
    public class FusionConfig()
    {
        [TomlPrecedingComment("If the time mode will be set to 'Level', when in a fusion lobby it will override the time to display how long you are in the lobby instead of the level")]
        [TomlProperty("OverrideTimeToLobby")]
        public bool OverrrideTimeToLobby { get; set; } = true;

        [TomlProperty("ShowJoinRequestPopUp")]
        public bool ShowJoinRequestPopUp { get; set; } = true;

        [TomlPrecedingComment("If true, when hosting a friends only / private server, players will be able to let others join the server through Discord")]
        [TomlProperty("AllowPlayersToInvite")]
        public bool AllowPlayersToInvite { get; set; } = true;

        [TomlProperty("LevelLoaded")]
        public RPCConfig LevelLoaded { get; set; } = new("%levelName%", "%fusion_lobbyName%");

        [TomlProperty("LevelLoading")]
        public RPCConfig LevelLoading { get; set; } = new("Loading %levelName%", "%fusion_lobbyName%");
    }
}