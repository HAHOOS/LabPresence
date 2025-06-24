using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

using BoneLib.Notifications;

using DiscordRPC;
using DiscordRPC.Message;

using Il2CppSLZ.Marrow.SceneStreaming;

using LabPresence.Config;
using LabPresence.Helper;

using MelonLoader;

using Semver;

using UnityEngine;

namespace LabPresence.Plugins.Default
{
    internal class FusionPlugin : Plugin<FusionConfig>
    {
        public override string Name => "Fusion";

        public override SemVersion Version => new(1, 0, 0);

        public override string Author => "HAHOOS";

        internal static FusionPlugin Instance { get; set; }

        public override void Init()
        {
            Instance = this;
            if (!Fusion.HasFusion)
            {
                Logger.Error("LabFusion is not installed, so FusionPlugin will not be set up!");
                return;
            }

            Placeholders.RegisterPlaceholder("fusion_lobbyName", (_) => Fusion.GetLobbyName());
            Placeholders.RegisterPlaceholder("fusion_host", (_) => Fusion.GetHost());
            Placeholders.RegisterPlaceholder("fusion_currentPlayers", (_) => Fusion.GetPlayerCount().current.ToString());
            Placeholders.RegisterPlaceholder("fusion_maxPlayers", (_) => Fusion.GetPlayerCount().max.ToString());
            Placeholders.RegisterPlaceholder("fusion_privacy", (_) => Enum.GetName(Fusion.GetPrivacy()).Replace("_", " "));

            Overwrites.OnLevelLoaded.RegisterOverwrite(OnLevelLoaded, out _, 100);
            Overwrites.OnLevelLoaded.RegisterOverwrite(OnLevelLoading, out _, 100);

            Overwrites.OnJoin.RegisterOverwrite(Join, out _, 100);
            Overwrites.OnJoinRequested.RegisterOverwrite(JoinRequested, out _, 100);

            MelonEvents.OnUpdate.Subscribe(Update);
            HasFusion();
        }

        private void HasFusion()
        {
            Fusion.Init(Logger);
            LabFusion.Utilities.MultiplayerHooking.OnDisconnected += OnDisconnect;
        }

        private void OnDisconnect()
        {
            if (RichPresenceManager.OverrideTimestamp?.Origin == "fusion")
                RichPresenceManager.ResetOverrideTimestamp();
            Overwrites.OnLevelLoaded.Run();
        }

        private bool JoinRequested(JoinRequestMessage message)
        {
            try
            {
                Logger.Info("Join requested");
                void after() => Fusion.JoinRequest(message);
                MelonCoroutines.Start(AfterLevelLoaded(after));
            }
            catch (Exception ex)
            {
                Notifier.Send(new Notification()
                {
                    Title = "Failure | LabPresence",
                    Message = "An unexpected error has occurred while handling join request, check the console or logs for more details",
                    Type = NotificationType.Error,
                    PopupLength = 5f,
                    ShowTitleOnPopup = true,
                });
                Logger.Error($"An unexpected error has occurred while handling join request, exception:\n{ex}");
                return false;
            }
            return true;
        }

        private bool OnLevelLoaded()
        {
            Logger.Info("level loaded");
            if (Fusion.IsConnected)
                RichPresenceManager.TrySetRichPresence(GetConfig().LevelLoaded, party: GetParty(), secrets: GetSecrets());
            return Fusion.IsConnected;
        }

        private bool OnLevelLoading()
        {
            if (Fusion.IsConnected)
                RichPresenceManager.TrySetRichPresence(GetConfig().LevelLoading, party: GetParty(), secrets: GetSecrets());
            return Fusion.IsConnected;
        }

        private bool Join(JoinMessage e)
        {
            if (!Fusion.HasFusion)
                return false;

            try
            {
                Logger.Info("Received Join Request");
                string decrypted = RichPresenceManager.Decrypt(e.Secret);
                string[] split = decrypted.Split("|");

                if (split.Length <= 1)
                    throw new Exception("Secret provided to join the lobby did not include all of the necessary info");

                if (split.Length > 2)
                    throw new Exception("Secret provided to join the lobby was invalid, the name of the network layer or code to the server may have contained the '|' character used to separate network layer & code, causing unexpected results");

                string layer = split[0];
                string code = split[1];

                void join()
                {
                    Logger.Info($"Attempting to join with the following code: {code}");
                    if (code != Fusion.GetLobbyCode())
                    {
                        Notifier.Send(new Notification()
                        {
                            Title = "LabPresence",
                            Message = "Attempting to join the target lobby, this might take a few seconds...",
                            PopupLength = 4f,
                            ShowTitleOnPopup = true,
                            Type = NotificationType.Information
                        });

                        if (Fusion.EnsureNetworkLayer(layer))
                        {
                            Fusion.JoinByCode(code);
                        }
                        else
                        {
                            Notifier.Send(new Notification()
                            {
                                Title = "Failure | LabPresence",
                                Message = "Failed to ensure network layer, check the console/logs for errors. If none are present, it's likely the user is playing on a network layer that you do not have.",
                                Type = NotificationType.Error,
                                PopupLength = 5f,
                                ShowTitleOnPopup = true,
                            });
                        }
                    }
                    else
                    {
                        Logger.Error("Player is already in the lobby");
                        Notifier.Send(new Notification()
                        {
                            Title = "Failure | LabPresence",
                            Message = "Could not join, because you are already in the lobby!",
                            Type = NotificationType.Error,
                            PopupLength = 5f,
                            ShowTitleOnPopup = true,
                        });
                    }
                }

                MelonCoroutines.Start(AfterLevelLoaded(join));
            }
            catch (Exception ex)
            {
                Notifier.Send(new Notification()
                {
                    Title = "Failure | LabPresence",
                    Message = "An unexpected error has occurred while trying to join the lobby, check the console or logs for more details",
                    Type = NotificationType.Error,
                    PopupLength = 5f,
                    ShowTitleOnPopup = true,
                });
                Logger.Error($"An unexpected error has occurred while trying to join the lobby, exception:\n{ex}");
                return false;
            }
            return true;
        }

        private static Party GetParty()
        {
            if (!Fusion.IsConnected)
                return null;

            var id = Fusion.GetLobbyID();

            // Discord requires the ID string to have at least 2 characters
            if (id == 0 || id.ToString().Length < 2)
                return null;

            return new Party()
            {
                ID = Fusion.GetLobbyID().ToString(),
                Privacy = Fusion.GetPrivacy() == Fusion.ServerPrivacy.Public ? Party.PrivacySetting.Public : Party.PrivacySetting.Private,
                Max = Fusion.GetPlayerCount().max,
                Size = Fusion.GetPlayerCount().current
            };
        }

        private static Secrets GetSecrets()
        {
            if (!Fusion.IsConnected)
                return null;

            if (SceneStreamer.Session.Status == StreamStatus.LOADING)
                return null;

            var (current, max) = Fusion.GetPlayerCount();
            if (current >= max)
                return null;

            var privacy = Fusion.GetPrivacy();

            if (privacy == Fusion.ServerPrivacy.Locked)
                return null;

            if (privacy != Fusion.ServerPrivacy.Public && privacy != Fusion.ServerPrivacy.Friends_Only && !Fusion.IsAllowedToInvite())
                return null;

            var layer = Fusion.GetCurrentNetworkLayerTitle();
            if (string.IsNullOrWhiteSpace(layer))
                return null;

            var code = Fusion.GetLobbyCode();
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var encrypted = RichPresenceManager.Encrypt($"{layer}|{code}");

            return new Secrets()
            {
                JoinSecret = encrypted
            };
        }

        private static IEnumerator AfterLevelLoaded(Action callback)
        {
            while (SceneStreamer.Session?.Status != StreamStatus.DONE)
                yield return null;

            callback?.Invoke();
        }

        private float ElapsedSeconds = 0;

        private void Update()
        {
            if (Category != null && GetConfig() != null)
                Fusion.EnsureMetaDataSync();

            ElapsedSeconds += Time.deltaTime;

            var _delay = Math.Clamp(Core.Config.RefreshDelay, Core.RequiredDelay, double.MaxValue);
            _delay = Math.Clamp(_delay, Fusion.GetGamemodeMinimumDelay(), double.MaxValue);
            if (ElapsedSeconds >= _delay)
            {
                ElapsedSeconds = 0;

                if (Fusion.IsConnected && SceneStreamer.Session?.Status == StreamStatus.DONE)
                {
                    var (key, tooltip) = Fusion.GetGamemodeRPC();
                    RichPresenceManager.TrySetRichPresence(RichPresenceManager.CurrentConfig, ActivityType.Playing, GetParty(), GetSecrets(), smallImage: new(key, tooltip));
                }
            }
        }
    }

    /// <summary>
    /// Class that contains methods to interact with LabFusion
    /// </summary>
    public static class Fusion
    {
        private const string AllowKey = "LabPresence.AllowInvites";

        /// <summary>
        /// Is LabFusion installed
        /// </summary>
        public static bool HasFusion => MelonBase.FindMelon("LabFusion", "Lakatrazz") != null;

        private static Logger Logger;

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
            return (ServerPrivacy)(int)current;
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
            return LabFusion.Network.NetworkLayerManager.Layer?.Title;
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
            if (!LabFusion.Network.NetworkInfo.IsHost)
                LabFusion.Player.LocalPlayer.Metadata.Metadata.TryRemoveMetadata(AllowKey);
            else if (!LabFusion.Player.LocalPlayer.Metadata.Metadata.TryGetMetadata(AllowKey, out string val) || !bool.TryParse(val, out bool value) || value != FusionPlugin.Instance.GetConfig().AllowPlayersToInvite)
                LabFusion.Player.LocalPlayer.Metadata.Metadata.TrySetMetadata(AllowKey, FusionPlugin.Instance.GetConfig().AllowPlayersToInvite.ToString());
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
            if (!FusionPlugin.Instance.GetConfig().Joins)
                return false;

            if (LabFusion.Network.NetworkInfo.IsHost)
                return true;

            if (LabFusion.Player.PlayerIDManager.GetHostID() == null)
                return true;

            if (!LabFusion.Entities.NetworkPlayerManager.TryGetPlayer(LabFusion.Player.PlayerIDManager.GetHostID(), out var host))
                return true;

            if (host == null)
                return true;

            if (string.IsNullOrWhiteSpace(host.PlayerID?.Metadata?.Metadata?.GetMetadata(AllowKey)))
                return true;

            return host.PlayerID?.Metadata?.Metadata?.GetMetadata(AllowKey) == bool.TrueString;
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
                Logger.Error($"Could find network layer '{title}'");
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
                Logger.Error($"An unexpected error has occurred while ensuring fusion is on the right network layer, exception:\n{ex}");
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

            if (LabFusion.Network.NetworkLayerManager.Layer.Matchmaker != null)
            {
                LabFusion.Network.NetworkLayerManager.Layer.Matchmaker.RequestLobbies(x =>
                {
                    LabFusion.Data.LobbyInfo targetLobby = null;

                    if (x.Lobbies != null)
                    {
                        foreach (var item in x.Lobbies)
                        {
                            var info = item.Metadata.LobbyInfo;
                            if (info?.LobbyCode == code)
                            {
                                targetLobby = info;
                                break;
                            }
                        }
                    }

                    if (targetLobby == null)
                    {
                        Logger.Error("The lobby was not found");
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
                            Logger.Warning("Could not find host, unable to verify if you can join the lobby (Privacy: Friends Only)");
                        }
                        else
                        {
                            if (!LabFusion.Network.NetworkLayerManager.Layer.IsFriend(host.LongId))
                            {
                                Logger.Error("The lobby is friends only and you are not friends with the host, cannot join");
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
                        Logger.Error("The lobby is locked, cannot join");
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

            if (FusionPlugin.Instance.GetConfig()?.ShowJoinRequestPopUp == true)
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
            LabFusion.UI.Popups.Notifier.Send(new LabFusion.UI.Popups.Notification()
            {
                Title = "Join Request",
                Message = $"{message.User.DisplayName} (@{message.User.Username}) wants to join you!",
                PopupLength = 5f,
                SaveToMenu = true,
                ShowPopup = false,
                Type = LabFusion.UI.Popups.NotificationType.INFORMATION,
                OnAccepted = () => Core.Client.Respond(message, true),
                OnDeclined = () => Core.Client.Respond(message, false)
            });
        }

        internal static void Init(Logger logger)
        {
            if (HasFusion) Internal_Init(logger);
        }

        private static void Internal_Init(Logger logger)
        {
            Logger = logger;

            LabFusion.Utilities.MultiplayerHooking.OnDisconnected -= Update;
            LabFusion.Utilities.MultiplayerHooking.OnDisconnected += Update;

            LabFusion.Utilities.MultiplayerHooking.OnJoinedServer += SetTimestamp;
            LabFusion.Utilities.MultiplayerHooking.OnStartedServer += SetTimestamp;

            LabFusion.SDK.Gamemodes.GamemodeManager.OnGamemodeStarted += () => RichPresenceManager.SetOverrideTimestamp(new(GetGamemodeOverrideTime(), "fusion"), true);
            LabFusion.SDK.Gamemodes.GamemodeManager.OnGamemodeStopped += () =>
            {
                if (RichPresenceManager.OverrideTimestamp?.Origin == "fusion")
                    RichPresenceManager.ResetOverrideTimestamp(true);
            };

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
                var id = LabFusion.Player.PlayerIDManager.LocalID;

                List<LabFusion.Player.PlayerID> plrs = [.. LabFusion.Player.PlayerIDManager.PlayerIDs];
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
                return $"Team '{Core.RemoveUnityRichText(localPlayer.DisplayName)}' with {score} points and {(score > otherScore ? "winning!" : otherScore > score ? "losing :(" : "neither winning or losing..")}";
            });
            Gamemodes.RegisterGamemode("Lakatrazz.Entangled", () =>
            {
                if (LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode?.Barcode != "Lakatrazz.Entangled")
                    return string.Empty;

                var gamemode = (LabFusion.SDK.Gamemodes.Entangled)LabFusion.SDK.Gamemodes.GamemodeManager.ActiveGamemode;
                const string key = "InternalEntangledMetadata.Partner.{0}";
                bool success = gamemode.Metadata.TryGetMetadata(string.Format(key, LabFusion.Player.PlayerIDManager.LocalPlatformID), out string val);
                if (!success || val == "-1")
                {
                    return "With no partner :(";
                }
                else
                {
                    if (!ulong.TryParse(val, out ulong res))
                        return "With no partner :(";
                    var plr = LabFusion.Player.PlayerIDManager.GetPlayerID(res);
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
            if (FusionPlugin.Instance.GetConfig().OverwriteTimeToLobby && Core.Config.TimeMode == DefaultConfig.TimeModeEnum.Level)
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

            if (FusionPlugin.Instance.GetConfig().ShowCustomGamemodeToolTips)
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
                Logger.Error($"An unexpected error has occurred while trying to remotely get a key for the gamemode, defaulting to unknown key. Exception:\n{e}");
            }
            return "unknown_gamemode";
        }

        private static void Update()
        {
            if (Core.Config.TimeMode == DefaultConfig.TimeModeEnum.Level && FusionPlugin.Instance.GetConfig().OverwriteTimeToLobby && !IsConnected)
                RichPresenceManager.SetTimestampStartToNow();

            if (RichPresenceManager.CurrentConfig == FusionPlugin.Instance.GetConfig().LevelLoaded && !IsConnected)
                Overwrites.OnLevelLoaded.Run();
            else if (RichPresenceManager.CurrentConfig == FusionPlugin.Instance.GetConfig().LevelLoading && !IsConnected)
                Overwrites.OnLevelLoading.Run();
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