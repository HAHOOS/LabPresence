using MelonLoader;

using DiscordRPC;

using LabPresence.Helper;

using UnityEngine;

using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Marrow;

using MelonLoader.Utils;
using MelonLoader.Preferences;

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

using BoneLib;
using BoneLib.Notifications;
using System.Reflection;
using System.Collections.Generic;

namespace LabPresence
{
    public class Core : MelonMod
    {
        public const string Version = "1.0.0";

        public static DiscordRpcClient Client { get; private set; }

        private const string ClientID = "1338522973421965382";

        internal static MelonLogger.Instance Logger { get; private set; }

        internal static MelonPreferences_ReflectiveCategory Category { get; private set; }

        internal static MelonPreferences_ReflectiveCategory FusionCategory { get; private set; }

        internal static Config.DefaultConfig Config => Category?.GetValue<LabPresence.Config.DefaultConfig>();

        internal static Config.FusionConfig FusionConfig => FusionCategory?.GetValue<LabPresence.Config.FusionConfig>();

        public override void OnInitializeMelon()
        {
            if (HelperMethods.IsAndroid())
            {
                LoggerInstance.Error("This mod is not supported as it is unlikely for it to actually work.");
                this.Unregister("Unsupported platform", false);
                return;
            }

            Logger = LoggerInstance;

            LoggerInstance.Msg("Creating preferences");
            var dir = Directory.CreateDirectory(Path.Combine(MelonEnvironment.UserDataDirectory, "LabPresence"));
            Category = MelonPreferences.CreateCategory<LabPresence.Config.DefaultConfig>("LabPresenceConfig", "Lab Presence Config");
            Category.SetFilePath(Path.Combine(dir.FullName, "default.cfg"), true, false);
            Category.SaveToFile(false);

            FusionCategory = MelonPreferences.CreateCategory<LabPresence.Config.FusionConfig>("Fusion_LabPresenceConfig", "Fusion | Lab Presence Config");
            FusionCategory.SetFilePath(Path.Combine(dir.FullName, "fusion.cfg"), true, false);
            FusionCategory.SaveToFile(false);

            try
            {
                LoggerInstance.Msg("Adding README.txt");
                var assembly = Assembly.GetExecutingAssembly();
                var name = assembly?.GetName()?.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    const string path = "{0}.Embedded.README.txt";
                    using var stream = assembly.GetManifestResourceStream(string.Format(path, name));
                    using var reader = new StreamReader(stream);
                    var text = reader.ReadToEnd();
                    using var writer = File.CreateText(Path.Combine(dir.FullName, "README.txt"));
                    writer.Write(text);
                    writer.Flush();
                }
                else
                {
                    LoggerInstance.Warning("The assembly name could not be found! Cannot add README.txt");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"An unexpected error has occurred while creating README.txt, exception:\n{ex}");
            }

            if (Config.RefreshDelay <= 0.1)
            {
                LoggerInstance.Error("Hey, calm down. You shouldn't be spamming Discord servers! Although most of the requests won't be sent because they are identical presences, still please make it higher");
            }

            Fusion.Init();

            LoggerInstance.Msg("Adding placeholders");

            AddDefaultPlaceholders();

            LoggerInstance.Msg("Initializing...");

            Client = new DiscordRpcClient(ClientID, autoEvents: false)
            {
                Logger = new MLLogger(LoggerInstance, Config.RPCLogLevel)
            };

            Client.OnReady += (_, e) =>
            {
                LoggerInstance.Msg($"User @{e.User.Username} is ready");
                LoggerInstance.Msg("Registering URI Scheme");

                RegisterURIScheme();

                Client.SynchronizeState();
            };

            Client.OnConnectionEstablished += (_, e) => LoggerInstance.Msg($"Successfully established connection to pipe {e.ConnectedPipe}");
            Client.OnConnectionFailed += (_, e) => LoggerInstance.Error($"Failed to establish connection with pipe {e.FailedPipe}");
            Client.OnError += (_, e) => LoggerInstance.Error($"An unexpected error has occurred when sending a message, error: {e.Message}");

            Client.OnJoin += (_, e) =>
            {
                try
                {
                    LoggerInstance.Msg("Received Join Request");
                    string decrypted = Decrypt(e.Secret);
                    string[] split = decrypted.Split("|");

                    if (split.Length <= 1)
                        throw new Exception("Secret provided to join the lobby did not include all of the necessary info");

                    if (split.Length > 2)
                        throw new Exception("Secret provided to join the lobby was invalid, the name of the network layer or code to the server may have contained the '|' character used to separate network layer & code, causing unexpected results");

                    string layer = split[0];
                    string code = split[1];

                    void join()
                    {
                        LoggerInstance.Msg($"Attempting to join with the following code: {code}");
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
                            LoggerInstance.Error("Player is already in the lobby");
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
                    LoggerInstance.Error($"An unexpected error has occurred while trying to join the lobby, exception:\n{ex}");
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
                        Title = "Failure | LabPresence",
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

            //LevelHooks.Init();
            LevelHooks.OnLevelLoaded += (_) =>
            {
                if (Config.TimeMode == LabPresence.Config.DefaultConfig.TimeModeEnum.Level && !(Fusion.IsConnected && FusionConfig.OverrrideTimeToLobby))
                    RPC.SetTimestampStartToNow();

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

            if (Config.TimeMode != LabPresence.Config.DefaultConfig.TimeModeEnum.CurrentTime)
                RPC.SetTimestampStartToNow();
            else
                RPC.SetTimestampToCurrentTime();

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
            Placeholders.RegisterPlaceholder("levelName", (_) => SceneStreamer.Session?.Level?.Title ?? "N/A");
            Placeholders.RegisterPlaceholder("avatarName", (_) => Player.RigManager?.AvatarCrate?.Crate?.Title ?? "N/A");
            Placeholders.RegisterPlaceholder("platform", (_) => MelonUtils.CurrentPlatform == (MelonPlatformAttribute.CompatiblePlatforms)3 ? "Quest" : "PCVR");
            Placeholders.RegisterPlaceholder("mlVersion", (_) => AppDomain.CurrentDomain?.GetAssemblies()?.FirstOrDefault(x => x.GetName().Name == "MelonLoader")?.GetName()?.Version?.ToString() ?? "N/A");
            Placeholders.RegisterPlaceholder("health", (_) => (Player.RigManager?.health?.curr_Health)?.ToString() ?? "0", 4f);
            Placeholders.RegisterPlaceholder("maxHealth", (_) => (Player.RigManager?.health?.max_Health).ToString() ?? "0");
            Placeholders.RegisterPlaceholder("healthPercentange", (_) => $"{MathF.Floor((Player.RigManager?.health?.curr_Health ?? 0) / (Player.RigManager?.health?.max_Health ?? 0))}%", 4f);
            Placeholders.RegisterPlaceholder("fps", (_) => FPS.FramesPerSecond.ToString(), 4f);
            Placeholders.RegisterPlaceholder("operatingSystem", (_) => SystemInfo.operatingSystem);
            Placeholders.RegisterPlaceholder("codeModsCount", (_) => RegisteredMelons.Count.ToString());
            Placeholders.RegisterPlaceholder("modsCount", (_) =>
            {
                if (!AssetWarehouse.ready || AssetWarehouse.Instance == null)
                    return "0";

                return (AssetWarehouse.Instance.PalletCount() - AssetWarehouse.Instance.gamePallets.Count).ToString();
            });

            // Ammo
            // Not sure why would anyone wanna use this placeholder
            Placeholders.RegisterPlaceholder("ammoLight", (_) => AmmoInventory.Instance?._groupCounts["light"].ToString() ?? "0", 4f);
            Placeholders.RegisterPlaceholder("ammoMedium", (_) => AmmoInventory.Instance?._groupCounts["medium"].ToString() ?? "0", 4f);
            Placeholders.RegisterPlaceholder("ammoHeavy", (_) => AmmoInventory.Instance?._groupCounts["heavy"].ToString() ?? "0", 4f);

            // Fusion placeholders
            Placeholders.RegisterPlaceholder("fusion_lobbyName", (_) => Fusion.GetLobbyName());
            Placeholders.RegisterPlaceholder("fusion_host", (_) => Fusion.GetHost());
            Placeholders.RegisterPlaceholder("fusion_currentPlayers", (_) => Fusion.GetPlayerCount().current.ToString());
            Placeholders.RegisterPlaceholder("fusion_maxPlayers", (_) => Fusion.GetPlayerCount().max.ToString());
            Placeholders.RegisterPlaceholder("fusion_privacy", (_) => Enum.GetName(Fusion.GetPrivacy()).Replace("_", " "));
            Placeholders.RegisterPlaceholder("fusion_team_playerCount", (args) =>
            {
                if (args == null || args.Length != 1)
                    return "0";

                if (!Fusion.IsGamemodeStarted)
                    return "0";

                return Fusion.GetTeamPlayerCount(args[^1]);
            });

            // Test placeholder
            Placeholders.RegisterPlaceholder("test_multiply", (args) =>
            {
                if (args == null || args.Length == 0)
                    return "0";

                List<int> nums = [];
                foreach (var item in args)
                {
                    if (int.TryParse(item, out int res))
                        nums.Add(res);
                }
                int current = 1;
                nums.ForEach(x => current *= x);
                return current.ToString();
            });
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

        private static float _elapsedSecondsDateCheck = 0;

        public static string RemoveUnityRichText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return Regex.Replace(text, "<(.*?)>", string.Empty);
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

            if (Fusion.GetPrivacy() == Fusion.ServerPrivacy.Locked)
                return null;

            if (!Fusion.IsAllowedToInvite())
                return null;

            var layer = Fusion.GetCurrentNetworkLayerTitle();
            if (string.IsNullOrWhiteSpace(layer))
                return null;

            var code = Fusion.GetLobbyCode();
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var encrypted = Encrypt($"{layer}|{code}");

            return new Secrets()
            {
                JoinSecret = encrypted
            };
        }

        private string lastState, lastDetails;

        private float delay;

        private int lastDay = 0;

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Client.IsInitialized)
                Client?.Invoke();
            FPS.OnUpdate();
            LevelHooks.OnUpdate();
            if (FusionConfig != null)
                Fusion.EnsureMetaDataSync();

            _elapsedSeconds += Time.deltaTime;
            _elapsedSecondsDateCheck += Time.deltaTime;

            if (RPC.CurrentConfig != null)
            {
                if (_elapsedSecondsDateCheck >= 7.5f)
                {
                    _elapsedSecondsDateCheck = 0;
                    if (Config.TimeMode == LabPresence.Config.DefaultConfig.TimeModeEnum.CurrentTime)
                    {
                        var now = DateTime.Now;
                        if (now.Day != lastDay)
                        {
                            lastDay = now.Day;
                            RPC.SetTimestampToCurrentTime();
                        }
                    }
                }

                if (lastDetails != RPC.CurrentConfig.Details && lastState != RPC.CurrentConfig.State)
                {
                    lastDetails = RPC.CurrentConfig.Details;
                    lastState = RPC.CurrentConfig.State;
                    delay = RPC.CurrentConfig.GetMinimumDelay();
                }
                if (_elapsedSeconds >= Math.Clamp(Config.RefreshDelay, delay, double.MaxValue))
                {
                    _elapsedSeconds = 0;

                    if (!Fusion.IsConnected)
                    {
                        RPC.SetRPC(RPC.CurrentConfig);
                    }
                    else
                    {
                        var (key, tooltip) = Fusion.GetGamemodeRPC();
                        RPC.SetRPC(RPC.CurrentConfig, ActivityType.Playing, GetParty(), GetSecrets(), key, tooltip);
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