
This file contains all of the default placeholders that you can use to make your Rich Presence look even better!
Note that the placeholders are case-sensitive
If you want to escape a placeholder, write for example \%test\% which in the result will give %test%

=============================

%levelName% - Name of the level
%avatarName% - Name of the avatar
%platform% - If Android, Quest, otherwise PCVR
%mlVersion% - Current version of Melon Loader
%health% - Current health of the player
%maxHealth% - The maximum health of the player
%healthPercentage% - Percentage of health
%fps% - FPS of the game
%operatingSystem% - Name of the operating system
%codeModsCount% - Amount of all MelonLoader mods & plugins
%modsCount% - The amount of all loaded SDK mods

%ammoLight% - The amount of light ammo
%ammoMedium% - The amount of medium ammo
%ammoHeavy% - The amount of heavy ammo

%fusion_lobbyName% - Name of the current lobby, if the user is in no server, "N/A"
%fusion_host% - Name of the host of the current lobby, if the user is in no server, "N/A",
%fusion_currentPlayers% - The amount of the players currently in the lobby, 0 if in no server
%fusion_maxPlayers% - The amount of allowed players in the lobby, 0 if in no server
%fusion_privacy% - The privacy of the server (Public, Friends Only, Private etc.)

=============================

Currently there are a few placeholders that have a minimum delay set (4 seconds), because they change frequently.
These ones are:
	%health%
	%healthPercentage%
	%fps%
	%ammoLight%
	%ammoMedium%
	%ammoHeavy%

All of the other placeholders have no minimum delay. This delay will only be in effect when present in a config that's currently being displayed.
The Rich Presence may sometimes not update immediately due to a restriction set in place by Discord, only allowing for 5 requests per 20 seconds to be sent for updating the RPC.

If you'd like for a new placeholder to be added, DM me on Discord (@hahoos) and specify what you would like it to display.
Please note that I will not be adding every placeholder thats get suggested.

This also applies to suggestions that are not related to placeholders. It would be appreciated if you would DM me if you find any bugs!

