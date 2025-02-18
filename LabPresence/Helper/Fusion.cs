using System;
using System.Net.Http;

using DiscordRPC.Message;

using UnityEngine;
using System.Text.Json;

namespace LabPresence.Helper
{
    public static class Fusion
    {
        private const string AllowKey = "LabPresence.AllowInvites";

        public static bool HasFusion => Core.FindMelon("LabFusion", "Lakatrazz") != null;

        public static bool IsConnected
        {
            get
            {
                if (HasFusion)
                    return Internal_IsConnected();
                return false;
            }
        }

        internal static bool Internal_IsConnected()
        {
            return LabFusion.Network.NetworkInfo.HasServer;
        }

        public static string GetServerName()
        {
            if (!IsConnected) return "N/A";
            else return Internal_GetServerName();
        }

        internal static string Internal_GetServerName()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            var lobbyName = lobbyInfo?.LobbyName;
            if (lobbyInfo == null)
                return "N/A";
            return string.IsNullOrWhiteSpace(lobbyName) ? $"{lobbyInfo.LobbyHostName}'s server" : lobbyName;
        }

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

        public static ulong GetServerID()
        {
            if (!IsConnected) return 0;
            else return Internal_GetServerID();
        }

        private static ulong Internal_GetServerID()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            if (lobbyInfo == null)
                return 0;

            return lobbyInfo.LobbyId;
        }

        public static string GetServerCode()
        {
            if (!IsConnected) return string.Empty;
            else return Internal_GetServerCode();
        }

        private static string Internal_GetServerCode()
        {
            var lobbyInfo = LabFusion.Network.LobbyInfoManager.LobbyInfo;
            if (lobbyInfo == null)
                return string.Empty;

            return lobbyInfo.LobbyCode;
        }

        public static string GetCurrentNetworkLayerTitle()
        {
            if (!IsConnected) return null;
            else return Internal_GetCurrentNetworkLayerTitle();
        }

        private static string Internal_GetCurrentNetworkLayerTitle()
        {
            return LabFusion.Network.NetworkInfo.CurrentNetworkLayer?.Title;
        }

        public static void EnsureMetaDataSync()
        {
            if (IsConnected) Internal_EnsureMetadataSync();
        }

        internal static void Internal_EnsureMetadataSync()
        {
            if (!LabFusion.Network.NetworkInfo.IsServer)
                LabFusion.Player.LocalPlayer.Metadata.TryRemoveMetadata(AllowKey);
            else
                LabFusion.Player.LocalPlayer.Metadata.TrySetMetadata(AllowKey, Core.FusionConfig.AllowPlayersToInvite.ToString());
        }

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

            if (host.PlayerId?.Metadata?.GetMetadata(AllowKey) == null)
                return true;

            return host.PlayerId?.Metadata?.GetMetadata(AllowKey) == bool.TrueString;
        }

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

        public static void JoinByCode(string code)
        {
            if (HasFusion && !string.IsNullOrWhiteSpace(code))
                Internal_JoinByCode(code);
        }

        private static void Internal_JoinByCode(string code)
        {
            if (IsConnected)
                LabFusion.Network.NetworkHelper.Disconnect("Joining another server");

            LabFusion.Network.NetworkHelper.JoinServerByCode(code);
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
                Texture2D texture = RPC.GetAvatar(message.User) ?? new Texture2D(1, 1);
                BoneLib.Notifications.Notifier.Send(new BoneLib.Notifications.Notification()
                {
                    Title = "Join Request",
                    Message = $"{message.User.DisplayName} (@{message.User.Username}) wants to join you! Go to the fusion menu to accept or deny the quest",
                    PopupLength = 5f,
                    ShowTitleOnPopup = true,
                    Type = BoneLib.Notifications.NotificationType.CustomIcon,
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
        }

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

            return (GetGamemodeKey(gamemode.Barcode), gamemode.Title);
        }

        private static JsonDocument KnownGamemodesCache;

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
                    if (req.IsCompletedSuccessfully)
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
            if (RPC.CurrentConfig == Core.FusionConfig.LevelLoaded && !IsConnected)
                RPC.SetRPC(Core.Config.LevelLoaded);
            else if (RPC.CurrentConfig == Core.FusionConfig.LevelLoading && !IsConnected)
                RPC.SetRPC(Core.Config.LevelLoading);
        }

        public enum ServerPrivacy
        {
            Unknown = -1,
            Public = 0,
            Private = 1,
            Friends_Only = 2,
            Locked = 3,
        }
    }
}