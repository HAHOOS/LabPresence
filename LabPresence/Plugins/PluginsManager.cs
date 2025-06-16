using System;
using System.Collections.Generic;
using System.Linq;

namespace LabPresence.Plugins
{
    public static class PluginsManager
    {
        private static readonly List<IPlugin> _Plugins = [];

        public static IReadOnlyCollection<IPlugin> Plugins => _Plugins.AsReadOnly();

        public static void Register(Type plugin)
        {
            IPlugin instance = GetPluginFromType(plugin);

            if (IsRegistered(plugin))
                throw new Exception("A plugin with the same name is already registered!");

            Core.Logger.Msg($"Plugin '{instance.Name}' v{instance.Version} by {instance.Author} has been registered!");
            _Plugins.Add(instance);
            instance.Internal_Init();
        }

        public static void Register<PluginT>() where PluginT : IPlugin
            => Register(typeof(PluginT));

        public static bool Unregister(string name)
            => _Plugins.RemoveAll(x => x.Name == name) > 0;

        public static bool Unregister(Type plugin)
            => Unregister(GetPluginFromType(plugin).Name);

        public static bool Unregister<PluginT>() where PluginT : IPlugin
           => Unregister(GetPluginFromType(typeof(PluginT)).Name);

        public static bool IsRegistered(string name) => Plugins.Any(x => x.Name == name);

        public static bool IsRegistered(Type plugin) => IsRegistered(GetPluginFromType(plugin).Name);

        public static bool IsRegistered<PluginT>() where PluginT : IPlugin => IsRegistered(GetPluginFromType(typeof(PluginT)).Name);

        private static IPlugin GetPluginFromType(Type plugin)
        {
            if (!plugin.IsTypeOf<IPlugin>())
                throw new ArgumentException("Type must be a sub class of IPlugin!", nameof(plugin));

            return (IPlugin)Activator.CreateInstance(plugin);
        }

        public static bool IsTypeOf<T>(this Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }
    }
}