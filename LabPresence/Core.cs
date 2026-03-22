using MelonLoader;

using DiscordRPC;

using UnityEngine;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.SceneStreaming;

using MelonLoader.Utils;
using MelonLoader.Preferences;

using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using LabPresence.Plugins;
using LabPresence.Managers;
using LabPresence.Plugins.Default;

using BoneLib;
using Scriban.Runtime;
using LabPresence.Utilities;

// This entire code deservers to just be fucking removed, I wish that I NEVER have to work with it ever again
// This shit deserves to be coded from scratch
namespace LabPresence
{
    public class Core : MelonMod
    {
        public const string Version = "1.3.0";

        public static DiscordRpcClient Client { get; private set; }

        private const string ClientID = "1338522973421965382";

        internal static MelonLogger.Instance Logger { get; private set; }

        internal static MelonPreferences_ReflectiveCategory Category { get; private set; }

        internal static Config.DefaultConfig Config => Category?.GetValue<LabPresence.Config.DefaultConfig>();

        public static DateTimeOffset GameLaunch { get; } = DateTimeOffset.Now;

        public static DateTimeOffset LevelLaunch { get; private set; } = DateTimeOffset.Now;

        public static Thunderstore Thunderstore { get; private set; }

        public static bool FirstLevelLoad { get; private set; }

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

            LoggerInstance.Msg("Initializing Thunderstore");
            Thunderstore = new Thunderstore($"LabPresence / {Version} A BONELAB Mod");
            Thunderstore.BL_FetchPackage("LabPresence", "HAHOOS", Version, LoggerInstance);

            LoggerInstance.Msg("Adding placeholders");

            AddDefaultPlaceholders();

            LoggerInstance.Msg("Initializing...");

            Client = new DiscordRpcClient(ClientID, autoEvents: false)
            {
                Logger = new Logger(LoggerInstance, Config.RPCLogLevel, "RPC")
            };

            Client.OnReady += (_, e) =>
            {
                LoggerInstance.Msg($"User @{e.User.Username} is ready");
                LoggerInstance.Msg("Registering URI Scheme");

                RegisterURIScheme();

                Client.SynchronizeState();
            };

            Client.OnConnectionEstablished += (_, _) => LoggerInstance.Msg("Successfully established connection");
            Client.OnConnectionFailed += (_, e) => LoggerInstance.Error($"Failed to establish connection with pipe {e.FailedPipe}");
            Client.OnError += (_, e) => LoggerInstance.Error($"An unexpected error has occurred when sending a message, error: {e.Message}");

            Client.OnJoin += (_, e) => Overwrites.OnJoin.Run(e);
            Client.OnJoinRequested += (_, e) => Overwrites.OnJoinRequested.Run(e);
            Client.OnSpectate += (_, e) => Overwrites.OnSpectate.Run(e);

            Client.SkipIdenticalPresence = true;
            Client.SetSubscription(EventType.Join | EventType.JoinRequest | EventType.Spectate);

            Client.Initialize();

            Overwrites.OnLevelLoaded.SetDefault(() => RichPresenceManager.TrySetRichPresence(Config.LevelLoaded));
            Overwrites.OnLevelLoading.SetDefault(() => RichPresenceManager.TrySetRichPresence(Config.LevelLoading));
            Overwrites.OnAssetWarehouseLoaded.SetDefault(() => RichPresenceManager.TrySetRichPresence(Config.AssetWarehouseLoaded));
            Overwrites.OnPreGame.SetDefault(() => RichPresenceManager.TrySetRichPresence(Config.PreGameStarted));
            try
            {
                if (Fusion.HasFusion)
                    PluginsManager.Register<FusionPlugin>();
            }
            catch (Exception ex)
            {
                Logger.Error($"An unexpected error has occurred while attempting to register the Fusion Plugin, exception:\n{ex}");
            }

            LevelHooks.OnLevelLoaded += (_) =>
            {
                if (!FirstLevelLoad)
                {
                    FirstLevelLoad = true;
                    Thunderstore.BL_SendNotification();
                }

                LevelLaunch = DateTime.Now;
                if (Config.TimeMode == LabPresence.Config.TimeMode.Level)
                    ConfigureTimestamp(false);

                Overwrites.OnLevelLoaded.Run();
            };

            LevelHooks.OnLevelLoading += (_) => Overwrites.OnLevelLoading.Run();
            LevelHooks.OnLevelUnloaded += (_) => Overwrites.OnLevelUnloaded.Run();

            AssetWarehouse.OnReady((Il2CppSystem.Action)Overwrites.OnAssetWarehouseLoaded.Run);

            Overwrites.OnPreGame.Run();

            LoggerInstance.Msg("Creating BoneMenu");
            MenuManager.Init();

            LoggerInstance.Msg("Initialized.");
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

        public static void ConfigureTimestamp(bool autoUpdate = false)
        {
            if (Config.TimeMode == LabPresence.Config.TimeMode.CurrentTime)
                RichPresenceManager.SetTimestampToCurrentTime(autoUpdate);
            else if (Config.TimeMode == LabPresence.Config.TimeMode.GameSession)
                RichPresenceManager.SetTimestamp((ulong)GameLaunch.ToUnixTimeMilliseconds(), null, autoUpdate);
            else if (Config.TimeMode == LabPresence.Config.TimeMode.Level)
                RichPresenceManager.SetTimestamp((ulong)LevelLaunch.ToUnixTimeMilliseconds(), null, autoUpdate);
        }

        public static SpawnableCrate GetInHand(Handedness handType)
        {
            var hand = handType == Handedness.LEFT ? Player.LeftHand : Player.RightHand;
            return hand?.AttachedReceiver?.Host?.GetGrip()?._marrowEntity?._poolee?.SpawnableCrate;
        }

        private static void AddDefaultPlaceholders()
        {
            PlaceholderManager.RegisterPlaceholder("default", () =>
            {
                var scriptObject = new ScriptObject(StringComparer.OrdinalIgnoreCase)
                {
                    { "game", new ScribanGame() },
                    { "player", new ScribanPlayer() },
                    { "ammo", new ScribanAmmo() }
                };

                return scriptObject;
            });
        }

        public static string CleanLevelName()
        {
            var level = SceneStreamer.Session?.Level?.Title;
            if (level == null)
                return "N/A";

            if (Config.RemoveLevelNumbers)
                level = RemoveBONELABLevelNumbers(level);

            return level;
        }

        private static float _elapsedSecondsDateCheck = 0;

        public static string RemoveUnityRichText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return Regex.Replace(text, "<(.*?)>", string.Empty);
        }

        public static string RemoveBONELABLevelNumbers(string levelName)
            => Regex.Replace(levelName, "[0-9][0-9] - ", string.Empty);

        private static string lastState, lastDetails;

        private static int lastDay = -1;

        public override void OnUpdate()
        {
            base.OnUpdate();

            Internal_OnUpdate();
        }

        private static void Internal_OnUpdate()
        {
            if (Client?.IsInitialized == true)
                Client?.Invoke();

            RichPresenceManager.OnUpdate();
            Fps.OnUpdate();
            LevelHooks.OnUpdate();

            _elapsedSecondsDateCheck += Time.deltaTime;

            if (lastDay == -1)
            {
                var time = DateTime.Now;
                lastDay = time.Day;
            }

            if (RichPresenceManager.CurrentConfig != null)
            {
                if (_elapsedSecondsDateCheck >= 2f)
                {
                    _elapsedSecondsDateCheck = 0;
                    if (Config.TimeMode == LabPresence.Config.TimeMode.CurrentTime)
                    {
                        var now = DateTime.Now;
                        if (now.Day != lastDay)
                        {
                            lastDay = now.Day;
                            RichPresenceManager.SetTimestampToCurrentTime(true);
                        }
                    }
                }

                if (lastDetails != RichPresenceManager.CurrentConfig.Details && lastState != RichPresenceManager.CurrentConfig.State)
                {
                    lastDetails = RichPresenceManager.CurrentConfig.Details;
                    lastState = RichPresenceManager.CurrentConfig.State;
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