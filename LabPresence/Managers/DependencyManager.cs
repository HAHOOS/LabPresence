using System;
using System.Linq;
using Mono.Cecil;
using System.IO;
using MelonLoader.Utils;
using MelonLoader;
using System.Reflection;

namespace LabPresence.Managers
{
    internal static class DependencyManager
    {
        internal static bool TryLoadDependency(string name, bool log = true, bool createFile = true)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                if (assembly != null)
                {
                    var assemblyInfo = assembly.GetName();
                    if (assemblyInfo != null)
                    {
                        var _path = $"{assemblyInfo.Name}.Dependencies.{name}.dll";
                        var names = assembly.GetManifestResourceNames();
                        if (names == null || names.Length == 0 || !names.Contains(_path))
                        {
                            if (log) MelonLogger.Error($"There were no embedded resources or dependency was not found in the list of embedded resources, cannot not load {name}");
                            return false;
                        }
                        else
                        {
                            var stream = assembly.GetManifestResourceStream(_path);
                            if (stream?.Length > 0)
                            {
                                stream.Position = 0;
                                var assemblyMono = AssemblyDefinition.ReadAssembly(stream);
                                if (assemblyMono != null && !AppDomain.CurrentDomain.GetAssemblies().Any(x => x.GetName().Name == assemblyMono.Name.Name))
                                {
                                    if (createFile)
                                    {
                                        if (log) MelonLogger.Msg($"Creating {name}.dll in UserLibs");
                                        string path = Path.Combine(MelonEnvironment.UserLibsDirectory, $"{name}.dll");
                                        if (!File.Exists(path))
                                        {
                                            using (var fileStream = File.Create(path))
                                            {
                                                stream.Seek(0, SeekOrigin.Begin);
                                                stream.CopyTo(fileStream);
                                            }
                                            Assembly.LoadFile(path);
                                        }
                                        else
                                        {
                                            if (log) MelonLogger.Warning($"{name}.dll already exists");
                                        }
                                    }
                                    else
                                    {
                                        byte[] bytes = [];
                                        while (true)
                                        {
                                            var _byte = stream.ReadByte();
                                            if (_byte == -1)
                                                break;
                                            bytes[bytes.Length] = (byte)_byte;
                                        }
                                        Assembly.Load(bytes);
                                        bytes = null;
                                    }
                                    if (log) MelonLogger.Msg($"Loaded {name}");
                                }
                                else
                                {
                                    if (log) MelonLogger.Msg($"{name} is already loaded!");
                                }
                            }
                            else
                            {
                                if (log) MelonLogger.Error($"Could not get stream of {name}, cannot not load it");
                                return false;
                            }
                        }
                    }
                    else
                    {
                        if (log) MelonLogger.Error($"Assembly Info was not found, cannot not load {name}");
                        return false;
                    }
                }
                else
                {
                    if (log) MelonLogger.Error($"Executing assembly was somehow not found, cannot not load {name}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"An unexpected error occurred while loading {name}", ex);
                return false;
            }
            return true;
        }
    }
}