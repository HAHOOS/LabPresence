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
        /// <summary>
        /// Name for the <see cref="Plugin"/> which will be used in the logger and the BoneMenu
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Version of the <see cref="Plugin"/>
        /// </summary>
        public abstract SemVersion Version { get; }

        /// <summary>
        /// Author of the <see cref="Plugin"/>
        /// </summary>
        public abstract string Author { get; }

        /// <summary>
        /// Whether or not should the <see cref="Plugin"/> create a page in the LabPresence's BoneMenu
        /// </summary>
        public virtual bool CreatesMenu => false;

        /// <summary>
        /// The color of the <see cref="Plugin"/>'s page in the BoneMenu, only used if <see cref="CreatesMenu"/> is true"/>
        /// </summary>
        public virtual Color MenuColor => Color.white;

        /// <summary>
        /// A <see cref="LabPresence.Logger"/> to print messages to console
        /// </summary>
        public Logger Logger { get; internal set; }

        /// <summary>
        /// The page of the Plugin in BoneMenu, null if <see cref="CreatesMenu"/> is false.
        /// </summary>
        public Page MenuPage { get; internal set; }

        /// <summary>
        /// DO NOT run if you don't know what you're doing. This is run only once by the mod, don't use it
        /// </summary>
        public void Internal_Init();

        internal void Internal_PopulateMenu()
        {
            if (MenuPage != null || !CreatesMenu)
                return;
            MenuPage = MenuManager.PluginsPage.CreatePage(Name, MenuColor);
            PopulateMenu(MenuPage);
        }

        /// <summary>
        /// Populates the BoneMenu, only run if <see cref="CreatesMenu"/> is true
        /// </summary>
        public virtual void PopulateMenu(Page page)
        {
        }
    }

    /// <summary>
    /// Class for creating plugins
    /// </summary>
    public abstract class Plugin : IPlugin
    {
        /// <inheritdoc cref="IPlugin.Name"/>
        public abstract string Name { get; }

        /// <inheritdoc cref="IPlugin.Version"/>
        public abstract SemVersion Version { get; }

        /// <inheritdoc cref="IPlugin.Author"/>
        public abstract string Author { get; }

        /// <inheritdoc cref="IPlugin.CreatesMenu"/>
        public virtual bool CreatesMenu => false;

        /// <inheritdoc cref="IPlugin.MenuColor"/>
        public virtual Color MenuColor => Color.white;

        /// <summary>
        /// The category to set up configuration in
        /// </summary>
        public MelonPreferences_Category Category { get; private set; }

        /// <inheritdoc cref="IPlugin.Logger"/>
        public Logger Logger { get; set; }

        /// <inheritdoc cref="IPlugin.MenuPage"/>
        public Page MenuPage { get; set; }

        /// <summary>
        /// DO NOT run if you don't know what you're doing. This is run only once by the mod, don't use it
        /// </summary>
        public void Internal_Init()
        {
            Logger = new(Core.Logger, Name);
            Category = MelonPreferences.CreateCategory($"LabPresence_{Name}_Config");
            Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "LabPresence", $"{Name.ToLower()}.cfg"));
            Category.SaveToFile(false);
            Init();
        }

        /// <summary>
        /// Method that gets called when registered
        /// </summary>
        public virtual void Init()
        {
        }

        /// <inheritdoc cref="IPlugin.PopulateMenu(Page)"/>
        public virtual void PopulateMenu(Page page)
        {
        }
    }

    /// <summary>
    /// Class for creating plugins with the category being a <see cref="MelonPreferences_ReflectiveCategory"/>
    /// </summary>
    public abstract class Plugin<ConfigT> : IPlugin where ConfigT : new()
    {
        /// <inheritdoc cref="IPlugin.Name"/>
        public abstract string Name { get; }

        /// <inheritdoc cref="IPlugin.Version"/>
        public abstract SemVersion Version { get; }

        /// <inheritdoc cref="IPlugin.Author"/>
        public abstract string Author { get; }

        /// <inheritdoc cref="IPlugin.Logger"/>
        public Logger Logger { get; set; }

        /// <inheritdoc cref="IPlugin.MenuPage"/>
        public Page MenuPage { get; set; }

        /// <inheritdoc cref="IPlugin.CreatesMenu"/>
        public virtual bool CreatesMenu => false;

        /// <inheritdoc cref="IPlugin.MenuColor"/>
        public virtual Color MenuColor => Color.white;

        /// <summary>
        /// The category to set up configuration in
        /// </summary>
        public MelonPreferences_ReflectiveCategory Category { get; private set; }

        /// <summary>
        /// DO NOT run if you don't know what you're doing. This is run only once by the mod, dont use it
        /// </summary>
        public void Internal_Init()
        {
            Logger = new(Core.Logger, Name);
            Category = MelonPreferences.CreateCategory<ConfigT>($"LabPresence_{Name}_Config");
            Category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "LabPresence", $"{Name.ToLower()}.cfg"));
            Category.SaveToFile(false);
            Init();
        }

        /// <summary>
        /// Method that gets called when registered
        /// </summary>
        public virtual void Init()
        {
        }

        /// <inheritdoc cref="IPlugin.PopulateMenu(Page)"/>
        public virtual void PopulateMenu(Page page)
        {
        }

        /// <summary>
        /// Get the current value of <see cref="Category"/>
        /// </summary>
        public ConfigT GetConfig()
            => Category.GetValue<ConfigT>();
    }
}