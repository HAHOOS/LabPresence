using MelonLoader;

using DiscordRPC;

using LabPresence.Helper;

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
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using LabPresence.Plugins;
using LabPresence.Managers;
using LabPresence.Plugins.Default;

using BoneLib;
using Scriban.Runtime;

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
        public const string Version = "1.2.0";

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
                if (Config.TimeMode == LabPresence.Config.TimeMode.Level)
                    RichPresenceManager.SetTimestampStartToNow();

                Overwrites.OnLevelLoaded.Run();
            };

            LevelHooks.OnLevelLoading += (_) => Overwrites.OnLevelLoading.Run();
            LevelHooks.OnLevelUnloaded += (_) => Overwrites.OnLevelUnloaded.Run();

            AssetWarehouse.OnReady((Il2CppSystem.Action)Overwrites.OnAssetWarehouseLoaded.Run);

            if (Config.TimeMode != LabPresence.Config.TimeMode.CurrentTime)
                RichPresenceManager.SetTimestampStartToNow();
            else
                RichPresenceManager.SetTimestampToCurrentTime();

            Overwrites.OnPreGame.Run();

            LoggerInstance.Msg("Initialized.");
        }

        /// <summary>
        /// Runs before the melon gets initialized
        /// </summary>
        public override void OnEarlyInitializeMelon()
        {
            base.OnEarlyInitializeMelon();

            Load();
        }

        internal static void Load()
        {
            DependencyManager.TryLoadDependency("DiscordRPC", true, false);
            DependencyManager.TryLoadDependency("Scriban.Signed", true, false);
            DependenciesLoaded = true;
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

        private static SpawnableCrate GetInHand(Handedness handType)
        {
            var hand = handType == Handedness.LEFT ? Player.LeftHand : Player.RightHand;
            return hand.AttachedReceiver?.Host?.GetGrip()?._marrowEntity?._poolee?.SpawnableCrate;
        }

        private static void AddDefaultPlaceholders()
        {
            PlaceholderManager.RegisterPlaceholder("default", () =>
            {
                var modsCount = 0;
                if (AssetWarehouse.ready && AssetWarehouse.Instance != null)
                    modsCount = (AssetWarehouse.Instance.PalletCount() - AssetWarehouse.Instance.gamePallets.Count);

                var scriptObject = new ScriptObject
                {
                    { "level", new ScribanCrate(SceneStreamer.Session?.Level)  },
                    { "levelName", CleanLevelName() },
                    { "platform", MelonUtils.CurrentPlatform == MelonPlatformAttribute.CompatiblePlatforms.ANDROID ? "Quest" : "PCVR" },
                    { "mlVersion", AppDomain.CurrentDomain?.GetAssemblies()?.FirstOrDefault(x => x.GetName().Name == "MelonLoader")?.GetName()?.Version?.ToString() ?? "N/A" },
                    { "health", Player.RigManager?.health?.curr_Health ?? 0 },
                    { "maxHealth", Player.RigManager?.health?.max_Health ?? 0  },
                    { "healthPercentage", MathF.Floor(((Player.RigManager?.health?.curr_Health ?? 0) / (Player.RigManager?.health?.max_Health ?? 0)) * 100) },
                    { "fps", FPS.FramesPerSecond },
                    { "operatingSystem", SystemInfo.operatingSystem },
                    { "avatar", new ScribanCrate(Player.RigManager?.AvatarCrate?.Crate) },
                    { "avatarName", RemoveUnityRichText(Player.RigManager?.AvatarCrate?.Crate?.Title ?? "N/A")  },
                    { "leftHand", new ScribanCrate(GetInHand(Handedness.LEFT))  },
                    { "rightHand", new ScribanCrate(GetInHand(Handedness.RIGHT)) },
                    { "codeModsCount", RegisteredMelons.Count },
                    { "modsCount", modsCount },
                    { "ammoLight", AmmoInventory.Instance?._groupCounts["light"] ?? 0 },
                    { "ammoMedium", AmmoInventory.Instance?._groupCounts["medium"] ?? 0  },
                    { "ammoHeavy", AmmoInventory.Instance?._groupCounts["heavy"] ?? 0 }
                };

                return scriptObject;
            });
        }

        private static string CleanLevelName()
        {
            var level = SceneStreamer.Session?.Level?.Title;
            if (level == null)
                return "N/A";

            if (Config.RemoveLevelNumbers)
                level = RemoveBONELABLevelNumbers(level);

            return level;
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

        private static string lastState, lastDetails;
        public static bool DependenciesLoaded { get; private set; }

        private static int lastDay = -1;

        /// <summary>
        /// Runs every frame
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!DependenciesLoaded)
                return;

            Internal_OnUpdate();
        }

        private static void Internal_OnUpdate()
        {
            if (Client?.IsInitialized == true)
                Client?.Invoke();

            RichPresenceManager.OnUpdate();
            FPS.OnUpdate();
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