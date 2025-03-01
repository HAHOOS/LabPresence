using System;
using System.Net.Http;

using DiscordRPC.Message;

using UnityEngine;
using System.Text.Json;
using BoneLib.Notifications;
using NotificationType = BoneLib.Notifications.NotificationType;
using System.Linq;
using System.Collections.Generic;

namespace LabPresence.Helper
{
    /// <summary>
    /// Class that contains methods to interact with LabFusion
    /// </summary>
    public static class Fusion
    {
        private const string AllowKey = "LabPresence.AllowInvites";

        /// <summary>
        /// Is LabFusion installed
        /// </summary>
        public static bool HasFusion => Core.FindMelon("LabFusion", "Lakatrazz") != null;

        /// <summary>
        /// Is the local player connected to a lobby
        /// </summary>
        public static bool IsConnected
        {
            get
            {
                if (HasFusion)
                    return Internal_IsConnected();
                return false;
            }
        }

        /// <summary>
        /// Is a gamemode started
        /// </summary>
        public static bool IsGamemodeStarted
        {
            get
            {
                if (!IsConnected)
                    return false;
                return Internal_IsGamemodeStarted();
            }
        }

        private static bool Internal_IsGamemodeStarted()
        {
            return LabFusion.SDK.Gamemodes.GamemodeManager.IsGamemodeStarted;
        }

        internal static bool Internal_IsConnected()
        {
            return LabFusion.Network.NetworkInfo.HasServer;
        }

        /// <summary>
        /// Get the name of the current lobby
        /// </summary>
        public static string GetLobbyName()
        {
            if (!IsConnected) return "N/A";
            else return Internal_GetLobbyName();
        }

        internal static string Internal_GetLobbyName()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            var lobbyName = lobbyInfo?.LobbyName;
            if (lobbyInfo == null)
                return "N/A";
            return string.IsNullOrWhiteSpace(lobbyName) ? $"{lobbyInfo.LobbyHostName}'s lobby" : lobbyName;
        }

        /// <summary>
        /// Get the host of the current lobby
        /// </summary>
        public static string GetHost()
        {
            if (!IsConnected) return "N/A";
            else return Internal_GetHost();
        }

        internal static string Internal_GetHost()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            var host = lobbyInfo?.LobbyHostName;
            if (lobbyInfo == null)
                return "N/A";

            return string.IsNullOrWhiteSpace(host) ? "N/A" : host;
        }

        /// <summary>
        /// Get the player count of the current lobby
        /// </summary>
        public static (int current, int max) GetPlayerCount()
        {
            if (!IsConnected) return (-1, -1);
            else return Internal_GetPlayerCount();
        }

        private static (int current, int max) Internal_GetPlayerCount()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            if (lobbyInfo == null)
                return (-1, -1);

            var current = lobbyInfo.PlayerCount;
            var max = lobbyInfo.MaxPlayers;

            return (current, max);
        }

        /// <summary>
        /// Get the privacy of the current lobby
        /// </summary>
        public static ServerPrivacy GetPrivacy()
        {
            if (!IsConnected) return ServerPrivacy.Unknown;
            else return Internal_GetPrivacy();
        }

        private static ServerPrivacy Internal_GetPrivacy()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            if (lobbyInfo == null)
                return ServerPrivacy.Unknown;

            var current = lobbyInfo.Privacy;
            return (ServerPrivacy)((int)current);
        }

        /// <summary>
        /// Get the ID of the current lobby
        /// </summary>
        public static ulong GetLobbyID()
        {
            if (!IsConnected) return 0;
            else return Internal_GetLobbyID();
        }

        private static ulong Internal_GetLobbyID()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            if (lobbyInfo == null)
                return 0;

            return lobbyInfo.LobbyId;
        }

        /// <summary>
        /// Get the code of the current lobby
        /// </summary>
        public static string GetLobbyCode()
        {
            if (!IsConnected) return string.Empty;
            else return Internal_GetLobbyCode();
        }

        private static string Internal_GetLobbyCode()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            if (lobbyInfo == null)
                return string.Empty;

            return lobbyInfo.LobbyCode;
        }

        /// <summary>
        /// Get the name of the current network layer
        /// </summary>
        public static string GetCurrentNetworkLayerTitle()
        {
            if (!IsConnected) return null;
            else return Internal_GetCurrentNetworkLayerTitle();
        }

        private static string Internal_GetCurrentNetworkLayerTitle()
        {
            return LabFusion.Network.NetworkInfo.CurrentNetworkLayer?.Title;
        }

        /// <summary>
        /// Ensure that the metadata is up-to-date
        /// </summary>
        public static void EnsureMetaDataSync()
        {
            if (IsConnected) Internal_EnsureMetadataSync();
        }

        internal static void Internal_EnsureMetadataSync()
        {
            if (!LabFusion.Network.NetworkInfo.IsServer)
                LabFusion.Player.LocalPlayer.Metadata.TryRemoveMetadata(AllowKey);
            else if (!LabFusion.Player.LocalPlayer.Metadata.TryGetMetadata(AllowKey, out string val) || !bool.TryParse(val, out bool value) || value != Core.FusionConfig.AllowPlayersToInvite)
                LabFusion.Player.LocalPlayer.Metadata.TrySetMetadata(AllowKey, Core.FusionConfig.AllowPlayersToInvite.ToString());
        }

        /// <summary>
        /// Check if the host allows for people to invite
        /// </summary>
        public static bool IsAllowedToInvite()
        {
            if (!IsConnected)
                return false;
            else
                return Internal_IsAllowedToInvite();
        }

        private static bool Internal_IsAllowedToInvite()
        {
            if (LabFusion.Network.NetworkInfo.IsServer)
                return true;

            if (LabFusion.Player.PlayerIdManager.GetHostId() == null)
                return true;

            if (!LabFusion.Entities.NetworkPlayerManager.TryGetPlayer(LabFusion.Player.PlayerIdManager.GetHostId(), out var host))
                return true;

            if (host == null)
                return true;

            if (string.IsNullOrWhiteSpace(host.PlayerId?.Metadata?.GetMetadata(AllowKey)))
                return true;

            return host.PlayerId?.Metadata?.GetMetadata(AllowKey) == bool.TrueString;
        }

        /// <summary>
        /// Ensure you are on the right network layer
        /// </summary>
        /// <param name="title">The name of the network layer</param>
        /// <returns>Was the network layer switched successfully</returns>
        public static bool EnsureNetworkLayer(string title)
        {
            if (!HasFusion)
                return false;
            else
                return Internal_EnsureNetworkLayer(title);
        }

        private static bool Internal_EnsureNetworkLayer(string title)
        {
            if (!LabFusion.Network.NetworkLayer.LayerLookup.TryGetValue(title, out var layer))
            {
                Core.Logger.Error($"Could find network layer '{title}'");
                return false;
            }

            try
            {
                if (LabFusion.Network.NetworkLayerManager.LoggedIn && LabFusion.Network.NetworkLayerManager.Layer == layer)
                    return true;

                if (LabFusion.Network.NetworkLayerManager.LoggedIn)
                    LabFusion.Network.NetworkLayerManager.LogOut();

                LabFusion.Network.NetworkLayerManager.LogIn(layer);
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"An unexpected error has occurred while ensuring fusion is on the right network layer, exception:\n{ex}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Join a lobby by a code
        /// </summary>
        /// <param name="code">The code to use to join a lobby</param>
        public static void JoinByCode(string code)
        {
            if (HasFusion && !string.IsNullOrWhiteSpace(code))
                Internal_JoinByCode(code);
        }

        private static void Internal_JoinByCode(string code)
        {
            if (string.Equals(LabFusion.Network.NetworkHelper.GetServerCode(), code, StringComparison.OrdinalIgnoreCase))
            {
                Notifier.Send(new Notification()
                {
                    Title = "Error | LabPresence",
                    Message = "You are already in the lobby!",
                    PopupLength = 3.5f,
                    ShowTitleOnPopup = true,
                    Type = NotificationType.Error
                });
                return;
            }

            if (LabFusion.Network.NetworkInfo.CurrentNetworkLayer.Matchmaker != null)
            {
                LabFusion.Network.NetworkInfo.CurrentNetworkLayer.Matchmaker.RequestLobbies(x =>
                {
                    LabFusion.Data.LobbyInfo targetLobby = null;

                    if (x.lobbies != null)
                    {
                        foreach (var item in x.lobbies)
                        {
                            var info = item.metadata.LobbyInfo;
                            if (info?.LobbyCode == code)
                            {
                                targetLobby = info;
                                break;
                            }
                        }
                    }

                    if (targetLobby == null)
                    {
                        Core.Logger.Error("The lobby was not found");
                        Notifier.Send(new Notification()
                        {
                            Title = "Error | LabPresence",
                            Message = "The lobby you wanted to join was not found!",
                            PopupLength = 3.5f,
                            ShowTitleOnPopup = true,
                            Type = NotificationType.Error
                        });
                        return;
                    }

                    if (targetLobby.Privacy == LabFusion.Network.ServerPrivacy.FRIENDS_ONLY)
                    {
                        var host = targetLobby.PlayerList?.Players?.FirstOrDefault(x => x.Username == targetLobby.LobbyHostName);
                        if (host == null)
                        {
                            Core.Logger.Warning("Could not find host, unable to verify if you can join the lobby (Privacy: Friends Only)");
                        }
                        else
                        {
                            if (!LabFusion.Network.NetworkInfo.CurrentNetworkLayer.IsFriend(host.LongId))
                            {
                                Core.Logger.Error("The lobby is friends only and you are not friends with the host, cannot join");
                                Notifier.Send(new Notification()
                                {
                                    Title = "Error | LabPresence",
                                    Message = "Cannot join the lobby, because it is friends only and you are not friends with the host!",
                                    PopupLength = 3.5f,
                                    ShowTitleOnPopup = true,
                                    Type = NotificationType.Error
                                });
                                return;
                            }
                        }
                    }

                    if (targetLobby.Privacy == LabFusion.Network.ServerPrivacy.LOCKED)
                    {
                        Core.Logger.Error("The lobby is locked, cannot join");
                        Notifier.Send(new Notification()
                        {
                            Title = "Error | LabPresence",
                            Message = "Cannot join the lobby, because it is locked",
                            PopupLength = 3.5f,
                            ShowTitleOnPopup = true,
                            Type = NotificationType.Error
                        });
                        return;
                    }

                    if (IsConnected)
                        LabFusion.Network.NetworkHelper.Disconnect("Joining another lobby");

                    LabFusion.Network.NetworkHelper.JoinServerByCode(code);
                });
            }
            else
            {
                if (IsConnected)
                    LabFusion.Network.NetworkHelper.Disconnect("Joining another lobby");

                LabFusion.Network.NetworkHelper.JoinServerByCode(code);
            }
        }

        internal static void JoinRequest(JoinRequestMessage message)
        {
            if (IsConnected && message != null) Internal_JoinRequest(message);
        }

        private static void Internal_JoinRequest(JoinRequestMessage message)
        {
            if (message == null)
                return;

            if (Core.FusionConfig?.ShowJoinRequestPopUp == true)
            {
                Texture2D texture = RichPresenceManager.GetAvatar(message.User) ?? new Texture2D(1, 1);
                Notifier.Send(new Notification()
                {
                    Title = "Join Request",
                    Message = $"{message.User.DisplayName} (@{message.User.Username}) wants to join you! Go to the fusion menu to accept or deny the request",
                    PopupLength = 5f,
                    ShowTitleOnPopup = true,
                    Type = NotificationType.CustomIcon,
                    CustomIcon = texture
                });
            }
            LabFusion.Utilities.FusionNotifier.Send(new LabFusion.Utilities.FusionNotification()
            {
                Title = "Join Request",
                Message = $"{message.User.DisplayName} (@{message.User.Username}) wants to join you!",
                PopupLength = 5f,
                SaveToMenu = true,
                ShowPopup = false,
                Type = LabFusion.Utilities.NotificationType.INFORMATION,
                OnAccepted = () => Core.Client.Respond(message, true),
                OnDeclined = () => Core.Client.Respond(message, false)
            });
        }

        internal static void Init()
        {
            if (HasFusion) Internal_Init();
        }

        private static void Internal_Init()
        {
            LabFusion.Utilities.MultiplayerHooking.OnDisconnect -= Update;
            LabFusion.Utilities.MultiplayerHooking.OnDisconnect += Update;

            LabFusion.Utilities.MultiplayerHooking.OnJoinServer += SetTimestamp;
            LabFusion.Utilities.MultiplayerHooking.OnStartServer += SetTimestamp;

            Gamemodes.RegisterGamemode("Lakatrazz.Hide And Seek", () =>
            {
                if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode?.Barcode != "Lakatrazz.Hide And Seek")
                    return string.Empty;

                var gamemode = (LabFusion.SDK.Gamemodes.HideAndSeek)LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
                var team = gamemode.TeamManager.GetLocalTeam();
                return $"{team?.DisplayName ?? "N/A"} | {gamemode.HiderTeam.PlayerCount} hiders left!";
            });
            Gamemodes.RegisterGamemode("Lakatrazz.Deathmatch", () =>
            {
                if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode?.Barcode != "Lakatrazz.Deathmatch")
                    return string.Empty;

                var gamemode = (LabFusion.SDK.Gamemodes.Deathmatch)LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
                var id = LabFusion.Player.PlayerIdManager.LocalId;

                List<LabFusion.Player.PlayerId> plrs = [.. LabFusion.Player.PlayerIdManager.PlayerIds];
                plrs = [.. plrs.OrderBy(x => gamemode.ScoreKeeper.GetScore(x))];
                plrs.Reverse();

                return $"#{plrs.FindIndex(x => x.IsMe) + 1} place with {gamemode.ScoreKeeper.GetScore(id)} points!";
            });
            Gamemodes.RegisterGamemode("Lakatrazz.Team Deathmatch", () =>
            {
                if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode?.Barcode != "Lakatrazz.Team Deathmatch")
                    return string.Empty;

                var gamemode = (LabFusion.SDK.Gamemodes.TeamDeathmatch)LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
                var localPlayer = gamemode.TeamManager.GetLocalTeam();
                var score = gamemode.ScoreKeeper.GetScore(localPlayer);
                var otherScore = gamemode.ScoreKeeper.GetTotalScore() - score;
                return $"Team {Core.RemoveUnityRichText(localPlayer.DisplayName)} with {score} points and {(score > otherScore ? "winning!" : otherScore > score ? "losing :(" : "neither winning or losing..")}";
            });
            Gamemodes.RegisterGamemode("Lakatrazz.Entangled", () =>
            {
                if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode?.Barcode != "Lakatrazz.Entangled")
                    return string.Empty;

                var gamemode = (LabFusion.SDK.Gamemodes.Entangled)LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
                const string key = "InternalEntangledMetadata.Partner.{0}";
                bool success = gamemode.Metadata.TryGetMetadata(string.Format(key, LabFusion.Player.PlayerIdManager.LocalLongId), out string val);
                if (!success || val == "-1")
                {
                    return "With no partner :(";
                }
                else
                {
                    if (!ulong.TryParse(val, out ulong res))
                        return "With no partner :(";
                    var plr = LabFusion.Player.PlayerIdManager.GetPlayerId(res);
                    if (plr == null)
                        return "With no partner :(";

                    if (!LabFusion.Network.MetadataHelper.TryGetDisplayName(plr, out string name))
                        return "With no partner :(";

                    return $"Entangled with {Core.RemoveUnityRichText(name)}";
                }
            });
        }

        private static void SetTimestamp()
        {
            if (Core.FusionConfig.OverrrideTimeToLobby && Core.Config.TimeMode == Config.DefaultConfig.TimeModeEnum.Level)
                RichPresenceManager.SetTimestampStartToNow();
        }

        /// <summary>
        /// Get the override time of the current gamemode
        /// </summary>
        public static Timestamp GetGamemodeOverrideTime()
        {
            if (!IsConnected) return null;
            else return Internal_GetGamemodeOverrideTime();
        }

        private static Timestamp Internal_GetGamemodeOverrideTime()
        {
            if (!LabFusion.SDK.Gamemodes.GamemodeManager.IsGamemodeStarted)
                return null;

            if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode == null)
                return null;

            var gamemode = LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
            var registered = Gamemodes.GetGamemode(gamemode.Barcode);

            if (registered?.OverrideTime == null)
                return null;

            return registered.GetOverrideTime();
        }

        /// <summary>
        /// Get the minimum delay RPC refresh for the current gamemode
        /// </summary>
        public static float GetGamemodeMinimumDelay()
        {
            if (!IsConnected) return 0;
            else return Internal_GetGamemodeMinimumDelay();
        }

        private static float Internal_GetGamemodeMinimumDelay()
        {
            if (!LabFusion.SDK.Gamemodes.GamemodeManager.IsGamemodeStarted)
                return 0;

            var gamemode = LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
            if (gamemode == null)
                return 0;

            var registered = Gamemodes.GetGamemode(gamemode.Barcode);
            if (registered == null)
                return 0;

            var toolTip = registered.GetToolTipValue();
            if (string.IsNullOrWhiteSpace(toolTip))
                return 0;
            else
                return registered.MinimumDelay;
        }

        /// <summary>
        /// Get the small icon config for the current gamemode
        /// </summary>
        public static (string key, string tooltip) GetGamemodeRPC()
        {
            if (!IsConnected) return (null, null);
            else return Internal_GetGamemodeRPC();
        }

        private static (string key, string tooltip) Internal_GetGamemodeRPC()
        {
            if (!LabFusion.SDK.Gamemodes.GamemodeManager.IsGamemodeStarted)
                return (null, null);

            if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode == null)
                return (null, null);

            var gamemode = LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
            var registered = Gamemodes.GetGamemode(gamemode.Barcode);
            var val = registered?.CustomToolTip != null ? registered.GetToolTipValue() : string.Empty;

            if (Core.FusionConfig.ShowCustomGamemodeToolTips)
                return (GetGamemodeKey(gamemode.Barcode), !string.IsNullOrWhiteSpace(val) ? $"{gamemode.Title} | {val}" : gamemode.Title);
            else
                return (GetGamemodeKey(gamemode.Barcode), gamemode.Title);
        }

        private static JsonDocument KnownGamemodesCache;

        /// <summary>
        /// Get the small icon key for a gamemode
        /// </summary>
        /// <param name="barcode">Barcode of the gamemode</param>
        public static string GetGamemodeKey(string barcode)
        {
            try
            {
                const string knownGamemodes = "https://github.com/HAHOOS/LabPresence/blob/master/Data/gamemodes.json?raw=true";
                if (KnownGamemodesCache == null)
                {
                    var client = new HttpClient();
                    var req = client.GetAsync(knownGamemodes);
                    req.Wait();
                    if (req.IsCompletedSuccessfully && req.Result.IsSuccessStatusCode)
                    {
                        var content = req.Result.Content.ReadAsStringAsync();
                        content.Wait();
                        if (content.IsCompletedSuccessfully)
                        {
                            KnownGamemodesCache = JsonDocument.Parse(content.Result);
                        }
                    }
                }
                if (KnownGamemodesCache != null && KnownGamemodesCache.RootElement.TryGetProperty(barcode, out JsonElement val))
                    return val.GetString();
            }
            catch (Exception e)
            {
                Core.Logger.Error($"An unexpected error has occurred while trying to remotely get a key for the gamemode, defaulting to unknown key. Exception:\n{e}");
            }
            return "unknown_gamemode";
        }

        private static void Update()
        {
            if (Core.Config.TimeMode == Config.DefaultConfig.TimeModeEnum.Level && Core.FusionConfig.OverrrideTimeToLobby && !IsConnected)
                RichPresenceManager.SetTimestampStartToNow();

            if (RichPresenceManager.CurrentConfig == Core.FusionConfig.LevelLoaded && !IsConnected)
                RichPresenceManager.TrySetRichPresence(Core.Config.LevelLoaded);
            else if (RichPresenceManager.CurrentConfig == Core.FusionConfig.LevelLoading && !IsConnected)
                RichPresenceManager.TrySetRichPresence(Core.Config.LevelLoading);
        }

        /// <summary>
        /// The privacy of a lobby
        /// </summary>
        public enum ServerPrivacy
        {
            /// <summary>
            /// Unknown privacy
            /// </summary>
            Unknown = -1,

            /// <summary>
            /// Lobby is public
            /// </summary>
            Public = 0,

            /// <summary>
            /// Lobby is private, you can join only with a code
            /// </summary>
            Private = 1,

            /// <summary>
            /// Lobby is friends only, only friends of the host can join
            /// </summary>
            Friends_Only = 2,

            /// <summary>
            /// Lobby is locked, no one can join
            /// </summary>
            Locked = 3,
        }
    }
}