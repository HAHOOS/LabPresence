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
using System.Text.RegularExpressions;

using BoneLib;
using System.Reflection;
using System.Collections.Generic;
using LabPresence.Plugins.Default;
using LabPresence.Plugins;

namespace LabPresence
{
    /// <summary>
    /// Class that contains the core functionality
    /// </summary>
    public class Core : MelonMod
    {
        /// <summary>
        /// Version of the mod
        /// </summary>
        public const string Version = "1.1.0";

        /// <summary>
        /// The Discord RPC Client
        /// </summary>
        public static DiscordRpcClient Client { get; private set; }

        private const string ClientID = "1338522973421965382";

        internal static MelonLogger.Instance Logger { get; private set; }

        internal static MelonPreferences_ReflectiveCategory Category { get; private set; }

        internal static Config.DefaultConfig Config => Category?.GetValue<LabPresence.Config.DefaultConfig>();

        /// <summary>
        /// Runs when the melon gets initialized
        /// </summary>
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
                LoggerInstance.Error("Hey, calm down. You shouldn't be spamming Discord servers! Although most of the requests won't be sent because they are identical presences, still please make it higher");

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

            Client.OnConnectionEstablished += (_, e) => LoggerInstance.Msg($"Successfully established connection to pipe {e.ConnectedPipe}");
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

            //LevelHooks.Init();
            LevelHooks.OnLevelLoaded += (_) =>
            {
                if (Config.TimeMode == LabPresence.Config.DefaultConfig.TimeModeEnum.Level)
                    RichPresenceManager.SetTimestampStartToNow();

                Overwrites.OnLevelLoaded.Run();
            };

            LevelHooks.OnLevelLoading += (_) => Overwrites.OnLevelLoading.Run();

            AssetWarehouse.OnReady((Il2CppSystem.Action)Overwrites.OnAssetWarehouseLoaded.Run);

            var time = DateTime.Now;
            lastDay = time.Day;

            if (Config.TimeMode != LabPresence.Config.DefaultConfig.TimeModeEnum.CurrentTime)
                RichPresenceManager.SetTimestampStartToNow();
            else
                RichPresenceManager.SetTimestampToCurrentTime();

            Overwrites.OnPreGame.Run();

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

        private static void AddDefaultPlaceholders()
        {
            Placeholders.RegisterPlaceholder("levelName", (args) =>
            {
                var level = SceneStreamer.Session?.Level?.Title;
                if (level == null)
                    return "N/A";

                // The argument indicates if to remove numbers
                if (args?.Length > 0 && args[0] == bool.FalseString)
                    return level;

                if (Config.RemoveLevelNumbers)
                    level = RemoveBONELABLevelNumbers(level);

                return level;
            });
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

        private static float _elapsedSecondsDateCheck = 0;

        /// <summary>
        /// Remove Unity Rich Text from provided text
        /// </summary>
        /// <param name="text">The text to remove the rich text from</param>
        /// <returns>Text without Rich Text</returns>
        public static string RemoveUnityRichText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return Regex.Replace(text, "<(.*?)>", string.Empty);
        }

        /// <summary>
        /// Remove the funny numbers from BONELAB level names (example: '15 - Void G114' will output 'Void G114')
        /// </summary>
        /// <param name="levelName">The name of the level</param>
        /// <returns>Level name without the numbers</returns>
        public static string RemoveBONELABLevelNumbers(string levelName)
            => Regex.Replace(levelName, "[0-9][0-9] - ", string.Empty);

        private string lastState, lastDetails;

        public static float RequiredDelay { get; set; }

        private int lastDay = 0;

        /// <summary>
        /// Runs every frame
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (Client?.IsInitialized == true)
                Client?.Invoke();

            FPS.OnUpdate();
            LevelHooks.OnUpdate();

            _elapsedSecondsDateCheck += Time.deltaTime;

            if (RichPresenceManager.CurrentConfig != null)
            {
                if (_elapsedSecondsDateCheck >= 2f)
                {
                    _elapsedSecondsDateCheck = 0;
                    if (Config.TimeMode == LabPresence.Config.DefaultConfig.TimeModeEnum.CurrentTime)
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
                    RequiredDelay = RichPresenceManager.CurrentConfig.GetMinimumDelay();
                }
            }
        }

        /// <summary>
        /// Runs when application is quitting
        /// </summary>
        public override void OnApplicationQuit()
        {
            Client?.ClearPresence();
            Client?.Dispose();
        }
    }
}