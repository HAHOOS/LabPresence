using System.Reflection;

using MelonLoader;

#region MelonLoader

[assembly: MelonInfo(typeof(LabPresence.Core), "LabPresence", "1.0.0", "HAHOOS", null)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonPriority(-1000)]
[assembly: MelonOptionalDependencies("LabFusion")]

#endregion MelonLoader

#region Info

[assembly: AssemblyTitle("Adds Discord RPC to BONELAB")]
[assembly: AssemblyDescription("Adds Discord RPC to BONELAB")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: AssemblyProduct("LabPresence")]
[assembly: AssemblyCulture("")]

#endregion Info

#region Version

[assembly: AssemblyVersion(LabPresence.Core.Version)]
[assembly: AssemblyFileVersion(LabPresence.Core.Version)]
[assembly: AssemblyInformationalVersion(LabPresence.Core.Version)]

#endregion Version