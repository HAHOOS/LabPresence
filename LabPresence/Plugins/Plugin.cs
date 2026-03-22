using System.IO;

using BoneLib.BoneMenu;

using LabPresence.Managers;

using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;

using Semver;

using UnityEngine;

namespace LabPresence.Plugins
{
    public interface IPlugin
    {
        public abstract string Name { get; }

        public abstract SemVersion Version { get; }

        public abstract string Author { get; }

        public virtual bool CreatesMenu => false;

        public virtual Color MenuColor => Color.white;

        public Logger Logger { get; internal set; }

        public Page MenuPage { get; internal set; }

        public void Internal_Init();

        internal void Internal_PopulateMenu()
        {
            if (MenuPage != null || !CreatesMenu)
                return;
            MenuPage = MenuManager.PluginsPage.CreatePage(Name, MenuColor);

            MenuPage.CreateFunction($"Version: v{Version}", Color.white, null).SetProperty(ElementProperties.NoBorder);
            MenuPage.CreateFunction($"Author: {Author}", Color.white, null).SetProperty(ElementProperties.NoBorder);
            MenuPage.CreateFunction(string.Empty, Color.white, null).SetProperty(ElementProperties.NoBorder);

            PopulateMenu(MenuPage);
        }

        public virtual void PopulateMenu(Page page)
        {
        }
    }

    public abstract class Plugin : IPlugin
    {
        public abstract string Name { get; }

        public abstract SemVersion Version { get; }

        public abstract string Author { get; }

        public virtual bool CreatesMenu => false;

        public virtual Color MenuColor => Color.white;

        public MelonPreferences_Category Category { get; private set; }

        public Logger Logger { get; set; }

        public Page MenuPage { get; set; }

        public void Internal_Init()
        {
            Logger = new(Core.Logger, Name);
            Category = MelonPreferences.CreateCategory($"LabPresence_{Name}_Config");
            Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "LabPresence", $"{Name.ToLower()}.cfg"));
            Category.SaveToFile(false);
            Init();
        }

        public virtual void Init()
        {
        }

        public virtual void PopulateMenu(Page page)
        {
        }
    }

    public abstract class Plugin<ConfigT> : IPlugin where ConfigT : new()
    {
        public abstract string Name { get; }

        public abstract SemVersion Version { get; }

        public abstract string Author { get; }

        public Logger Logger { get; set; }

        public Page MenuPage { get; set; }

        public virtual bool CreatesMenu => false;

        public virtual Color MenuColor => Color.white;

        public MelonPreferences_ReflectiveCategory Category { get; private set; }

        public void Internal_Init()
        {
            Logger = new(Core.Logger, Name);
            Category = MelonPreferences.CreateCategory<ConfigT>($"LabPresence_{Name}_Config");
            Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "LabPresence", $"{Name.ToLower()}.cfg"));
            Category.SaveToFile(false);
            Init();
        }

        public virtual void Init()
        {
        }

        public virtual void PopulateMenu(Page page)
        {
        }

        public ConfigT GetConfig()
            => Category.GetValue<ConfigT>();
    }
}