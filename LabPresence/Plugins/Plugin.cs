using System.IO;

using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;

using Semver;

namespace LabPresence.Plugins
{
    public interface IPlugin
    {
        /// <summary>
        /// Name for the <see cref="Plugin"/> which will be used in the logger
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
        /// A <see cref="LabPresence.Logger"/> to print messages to console
        /// </summary>
        public Logger Logger { get; internal set; }

        /// <summary>
        /// DO NOT run if you don't know what you're doing. This is run only once by the mod, don't use it
        /// </summary>
        public void Internal_Init();
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

        /// <summary>
        /// The category to set up configuration in
        /// </summary>
        public MelonPreferences_Category Category { get; private set; }

        /// <inheritdoc cref="IPlugin.Logger"/>
        public Logger Logger { get; set; }

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

        public virtual void Init()
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

        public virtual void Init()
        {
        }

        /// <summary>
        /// Get the current value of <see cref="Category"/>
        /// </summary>
        public ConfigT GetConfig()
            => Category.GetValue<ConfigT>();
    }
}