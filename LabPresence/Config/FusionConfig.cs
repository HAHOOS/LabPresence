using Tomlet.Attributes;

namespace LabPresence.Config
{
    public class FusionConfig()
    {
        [TomlPrecedingComment("If the time mode will be set to 'Level', when in a fusion lobby it will override the time to display how long you are in the lobby instead of the level")]
        [TomlProperty("OverrideTimeToLobby")]
        public bool OverrideTimeToLobby { get; set; } = true;

        [TomlPrecedingComment("If true, a notification will be shown when someone requests to join your server")]
        [TomlProperty("ShowJoinRequestPopUp")]
        public bool ShowJoinRequestPopUp { get; set; } = true;

        [TomlPrecedingComment("If true, when hosting a private server, players will be able to let others join the server through Discord")]
        [TomlProperty("AllowPlayersToInvite")]
        public bool AllowPlayersToInvite { get; set; } = true;

        [TomlPrecedingComment("If true, gamemodes that support custom tooltips will display custom text on the small icon. Disabling this option will cause the tooltip to only show the name of the gamemode")]
        [TomlProperty("ShowCustomGamemodeToolTips")]
        public bool ShowCustomGamemodeToolTips { get; set; } = true;

        [TomlPrecedingComment("If true, the rich presence will allow discord users to join your server when available, otherwise if false, the join button will never be shown")]
        [TomlProperty("Joins")]
        public bool Joins { get; set; } = true;

        [TomlProperty("LevelLoaded")]
        public RPCConfig LevelLoaded { get; set; } = new("{{ levelName }}", "{{ fusion_lobbyName }}");

        [TomlProperty("LevelLoading")]
        public RPCConfig LevelLoading { get; set; } = new("Loading {{ levelName }}", "{{ fusion_lobbyName }}");
    }
}