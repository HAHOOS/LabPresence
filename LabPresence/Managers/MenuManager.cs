using BoneLib.BoneMenu;

using DiscordRPC.Logging;

using LabPresence.Config;
using LabPresence.Plugins;

using UnityEngine;

namespace LabPresence.Managers
{
    internal static class MenuManager
    {
        public static Page ModPage { get; private set; }

        public static Page ConfigPage { get; private set; }

        public static Page PluginsPage { get; private set; }

        public static bool IsInitialized { get; private set; }

        public static Color FromRGB(int r, int g, int b)
            => new(r / 255f, g / 255f, b / 255f);

        public static void Init()
        {
            ModPage = Page.Root.CreatePage("LabPresence", Color.cyan);
            Core.Thunderstore.BL_CreateMenuLabel(ModPage, false);
            ConfigPage = ModPage.CreatePage("Config", FromRGB(255, 172, 28)); // Orange Color
            PopulateConfig();

            PluginsPage = ModPage.CreatePage("Plugins", Color.cyan);
            PopulatePlugins();

            IsInitialized = true;
        }

        public static void PopulateConfig()
        {
            if (ConfigPage == null)
                return;

            ConfigPage.CreateEnum("RPC Log Level", FromRGB(255, 172, 28), Core.Config.RPCLogLevel, (val) => { Core.Config.RPCLogLevel = (LogLevel)val; Core.Category.SaveToFile(false); Core.Client?.Logger?.Level = (LogLevel)val; });
            ConfigPage.CreateEnum("Time Mode", FromRGB(191, 255, 0), Core.Config.TimeMode, (val) => { Core.Config.TimeMode = (TimeMode)val; Core.Category.SaveToFile(false); Core.ConfigureTimestamp(true); }).SetTooltip("What the Rich Presence will display as time, available options: Level (since the current level was loaded), CurrentTime (the current time, example: 15:53:50) and GameSession (since the game was launched)");
            ConfigPage.CreateBool("Remove Level Numbers", Color.red, Core.Config.RemoveLevelNumbers, (val) => { Core.Config.RemoveLevelNumbers = val; Core.Category.SaveToFile(false); }).SetTooltip("If true, in for example '15 - Void G114' the '15 - ' will be removed and only 'Void G114' will be shown in the 'levelName' placeholder");
        }

        public static void PopulatePlugins()
        {
            if (PluginsPage == null)
                return;
            foreach (var plugin in PluginsManager.Plugins)
            {
                if (!plugin.CreatesMenu)
                    continue;

                plugin.Internal_PopulateMenu();
            }
        }
    }
}