using MelonLoader;

using DiscordRPC;

using LabPresence.Helper;

using UnityEngine;

using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Marrow.SceneStreaming;

using MelonLoader.Utils;
using MelonLoader.Preferences;

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

using BoneLib;
using BoneLib.Notifications;
using Il2CppSLZ.Marrow;
using System.Linq;

[assembly: MelonInfo(typeof(LabPresence.Core), "LabPresence", "1.0.0", "HAHOOS", null)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonPriority(-1000)]

namespace LabPresence
{
    public class Core : MelonMod
    {
        public static DiscordRpcClient Client { get; private set; }

        private const string ClientID = "1338522973421965382";

        internal static MelonLogger.Instance Logger { get; private set; }

        internal static MelonPreferences_ReflectiveCategory Category { get; private set; }

        internal static MelonPreferences_ReflectiveCategory FusionCategory { get; private set; }

        internal static Config.DefaultConfig Config => Category?.GetValue<LabPresence.Config.DefaultConfig>();

        internal static Config.FusionConfig FusionConfig => FusionCategory?.GetValue<LabPresence.Config.FusionConfig>();

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;

            LoggerInstance.Msg("Creating preferences");
            var dir = Directory.CreateDirectory(Path.Combine(MelonEnvironment.UserDataDirectory, "LabPresence"));
            Category = MelonPreferences.CreateCategory<LabPresence.Config.DefaultConfig>("LabPresenceConfig", "Lab Presence Config");
            Category.SetFilePath(Path.Combine(dir.FullName, "default.cfg"), true, false);
            Category.SaveToFile(false);

            FusionCategory = MelonPreferences.CreateCategory<LabPresence.Config.FusionConfig>("Fusion_LabPresenceConfig", "Fusion | Lab Presence Config");
            FusionCategory.SetFilePath(Path.Combine(dir.FullName, "fusion.cfg"), true, false);
            FusionCategory.SaveToFile(false);

            Fusion.Init();

            LoggerInstance.Msg("Adding placeholders");

            AddDefaultPlaceholders();

            LoggerInstance.Msg("Initializing...");

            Client = new DiscordRpcClient(ClientID, autoEvents: false)
            {
                Logger = new MLLogger(LoggerInstance, DiscordRPC.Logging.LogLevel.Error)
            };

            Client.OnReady += (_, e) =>
            {
                LoggerInstance.Msg($"User @{e.User.Username} is ready");
                LoggerInstance.Msg("Registering URI Scheme");

                RegisterURIScheme();

                LoggerInstance.Msg("Setting subscriptions");
                Client.SynchronizeState();
            };

            Client.OnConnectionEstablished += (_, e) => LoggerInstance.Msg($"Successfully established connection to pipe {e.ConnectedPipe}");
            Client.OnConnectionFailed += (_, e) => LoggerInstance.Error($"Failed to establish connection with pipe {e.FailedPipe}");
            Client.OnError += (_, e) => LoggerInstance.Error($"An unexpected error has occurred when sending a message, error: {e.Message}");

            Client.OnJoin += (_, e) =>
            {
                try
                {
                    LoggerInstance.Msg("Joining lobby");
                    string decrypted = Decrypt(e.Secret);
                    string[] split = decrypted.Split("|");

                    if (split.Length <= 1)
                        throw new Exception("Secret provided to join the lobby did not include all of the necessary info");

                    string layer = split[0];
                    string code = split[1];

                    void join()
                    {
                        if (code != Fusion.GetServerCode())
                        {
                            if (Fusion.EnsureNetworkLayer(layer))
                            {
                                Fusion.JoinByCode(code);
                            }
                            else
                            {
                                Notifier.Send(new Notification()
                                {
                                    Title = "Failure",
                                    Message = "Failed to ensure network layer, check the console/logs for errors. If none are present, it's likely the user is playing on a network layer that you do not have.",
                                    Type = NotificationType.Error,
                                    PopupLength = 5f,
                                    ShowTitleOnPopup = true,
                                });
                            }
                        }
                    }

                    MelonCoroutines.Start(AfterLevelLoaded(join));
                }
                catch (Exception ex)
                {
                    Notifier.Send(new Notification()
                    {
                        Title = "Failure",
                        Message = "An unexpected error has occurred while trying to join the server, check the console or logs for more details",
                        Type = NotificationType.Error,
                        PopupLength = 5f,
                        ShowTitleOnPopup = true,
                    });
                    LoggerInstance.Error($"An unexpected error has occurred while trying to join the server, exception:\n{ex}");
                }
            };

            Client.OnJoinRequested += (_, e) =>
            {
                try
                {
                    LoggerInstance.Msg("Join requested");
                    void after() => Fusion.JoinRequest(e);
                    MelonCoroutines.Start(AfterLevelLoaded(after));
                }
                catch (Exception ex)
                {
                    Notifier.Send(new Notification()
                    {
                        Title = "Failure",
                        Message = "An unexpected error has occurred while handling join request, check the console or logs for more details",
                        Type = NotificationType.Error,
                        PopupLength = 5f,
                        ShowTitleOnPopup = true,
                    });
                    LoggerInstance.Error($"An unexpected error has occurred while handling join request, exception:\n{ex}");
                }
            };
            Client.SkipIdenticalPresence = true;
            Client.SetSubscription(EventType.Join | EventType.JoinRequest);

            Client.Initialize();

            Hooking.OnMarrowGameStarted += () => RPC.SetRPC(Config.MarrowGameStarted);

            //LevelHooks.Init();
            LevelHooks.OnLevelLoaded += (_) =>
            {
                if (!Fusion.IsConnected)
                    RPC.SetRPC(Config.LevelLoaded);
                else
                    RPC.SetRPC(FusionConfig.LevelLoaded);
            };

            LevelHooks.OnLevelLoading += (_) =>
            {
                if (!Fusion.IsConnected)
                    RPC.SetRPC(Config.LevelLoading);
                else
                    RPC.SetRPC(FusionConfig.LevelLoading);
            };

            AssetWarehouse.OnReady((Action)(() => RPC.SetRPC(Config.AssetWarehouseLoaded)));

            RPC.SetStartToNow();

            RPC.SetRPC(Config.PreGameStarted);

            LoggerInstance.Msg("Initialized.");
        }

        private static IEnumerator AfterLevelLoaded(Action callback)
        {
            while (SceneStreamer.Session?.Status != StreamStatus.DONE)
                yield return null;

            callback?.Invoke();
        }

        private void RegisterURIScheme()
        {
            try
            {
                Client.RegisterUriScheme();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"An unexpected error has occurred while registering URI scheme, exception:\n{ex}");
            }
        }

        private static void AddDefaultPlaceholders()
        {
            Placeholders.AddPlaceholder("levelName", () => SceneStreamer.Session?.Level?.Title ?? "N/A");
            Placeholders.AddPlaceholder("avatarName", () => Player.RigManager?.AvatarCrate?.Crate?.Title ?? "N/A");
            Placeholders.AddPlaceholder("platform", () => MelonUtils.CurrentPlatform == (MelonPlatformAttribute.CompatiblePlatforms)3 ? "Quest" : "PCVR");
            Placeholders.AddPlaceholder("mlVersion", () => AppDomain.CurrentDomain?.GetAssemblies()?.FirstOrDefault(x => x.GetName().Name == "MelonLoader")?.GetName()?.Version?.ToString() ?? "N/A");
            Placeholders.AddPlaceholder("health", () => (Player.RigManager?.health?.curr_Health)?.ToString() ?? "0");
            Placeholders.AddPlaceholder("maxHealth", () => (Player.RigManager?.health?.max_Health).ToString() ?? "0");
            Placeholders.AddPlaceholder("healthPercentange", () => $"{MathF.Floor((Player.RigManager?.health?.curr_Health ?? 0) / (Player.RigManager?.health?.max_Health ?? 0))}%");
            Placeholders.AddPlaceholder("fps", () => FPS.FramesPerSecond.ToString());
            Placeholders.AddPlaceholder("cpuUsage", () => SystemInfo.processorCount.ToString());
            Placeholders.AddPlaceholder("gpuUsage", () => SystemInfo.graphicsMemorySize.ToString());
            Placeholders.AddPlaceholder("gpuName", () => SystemInfo.graphicsDeviceName);
            Placeholders.AddPlaceholder("operatingSystem", () => SystemInfo.operatingSystem);
            Placeholders.AddPlaceholder("codeModsCount", () => RegisteredMelons.Count.ToString());
            Placeholders.AddPlaceholder("modsCount", () =>
            {
                if (!AssetWarehouse.ready || AssetWarehouse.Instance == null)
                    return "0";

                return (AssetWarehouse.Instance.PalletCount() - AssetWarehouse.Instance.gamePallets.Count).ToString();
            });

            // Ammo
            // Not sure why would anyone wanna use this placeholder
            Placeholders.AddPlaceholder("ammoLight", () => AmmoInventory.Instance?._groupCounts["light"].ToString() ?? "0");
            Placeholders.AddPlaceholder("ammoMedium", () => AmmoInventory.Instance?._groupCounts["medium"].ToString() ?? "0");
            Placeholders.AddPlaceholder("ammoHeavy", () => AmmoInventory.Instance?._groupCounts["heavy"].ToString() ?? "0");

            // Fusion placeholders
            Placeholders.AddPlaceholder("fusion_lobbyName", () => Fusion.GetServerName());
            Placeholders.AddPlaceholder("fusion_host", () => Fusion.GetHost());
            Placeholders.AddPlaceholder("fusion_currentPlayers", () => Fusion.GetPlayerCount().current.ToString());
            Placeholders.AddPlaceholder("fusion_maxPlayers", () => Fusion.GetPlayerCount().max.ToString());
            Placeholders.AddPlaceholder("fusion_privacy", () => Enum.GetName(Fusion.GetPrivacy()).Replace("_", " "));
        }

        private static string Encrypt(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return string.Empty;
            // Not the strongest but provides a bit of protection
            // I mean Discord says to encrypt it so I mean yeah, I'm doing that
            // Whatever
            var bytes = Encoding.UTF8.GetBytes(secret);
            return Convert.ToBase64String(bytes);
        }

        private static string Decrypt(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return string.Empty;

            var bytes = Convert.FromBase64String(secret);
            return Encoding.UTF8.GetString(bytes);
        }

        private static float _elapsedSeconds = 0;

        public static string RemoveUnityRichText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return Regex.Replace(text, "(?<!<noparse>)<(.*?)>(?!</noparse>)", string.Empty);
        }

        private static Party GetParty()
        {
            if (!Fusion.IsConnected)
                return null;

            var id = Fusion.GetServerID();

            // Discord requires the ID string to have at least 2 characters
            if (id == 0 || id.ToString().Length < 2)
                return null;

            return new Party()
            {
                ID = Fusion.GetServerID().ToString(),
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

            if (Fusion.GetPrivacy() == Fusion.ServerPrivacy.Locked)
                return null;

            if (!Fusion.IsAllowedToInvite())
                return null;

            var layer = Fusion.GetCurrentNetworkLayerTitle();
            if (string.IsNullOrWhiteSpace(layer))
                return null;

            var code = Fusion.GetServerCode();
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var encrypted = Encrypt($"{layer}|{code}");

            return new Secrets()
            {
                JoinSecret = encrypted
            };
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Client.IsInitialized)
                Client?.Invoke();
            FPS.OnUpdate();
            LevelHooks.OnUpdate();
            if (FusionConfig == null)
                Fusion.EnsureMetadataSync();

            _elapsedSeconds += Time.deltaTime;
            if (_elapsedSeconds >= Config.RefreshDelay)
            {
                _elapsedSeconds = 0;
                if (RPC.CurrentConfig != null)
                {
                    if (!Fusion.IsConnected)
                    {
                        RPC.SetRPC(RPC.CurrentConfig);
                    }
                    else
                    {
                        var (key, tooltip) = Fusion.GetGamemodeRPC();
                        RPC.SetRPC(RPC.CurrentConfig, GetParty(), GetSecrets(), key, tooltip);
                    }
                }
            }
        }

        public override void OnApplicationQuit()
        {
            Client?.ClearPresence();
            Client?.Dispose();
        }
    }
}