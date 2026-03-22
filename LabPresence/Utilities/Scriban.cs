using System;
using System.Collections.Generic;
using System.Linq;

using BoneLib;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Marrow.Warehouse;

using MelonLoader;

using Scriban.Runtime;

using UnityEngine;

namespace LabPresence.Utilities
{
    public class ScribanCrate
    {
        public CrateType Type { get; }

        public string Barcode { get; }

        public string Title { get; }

        public string Description { get; }

        public bool Redacted { get; }

        public bool Unlockable { get; }

        public ScriptArray<string> Tags { get; }

        public ScriptArray<ScribanBoneTag> BoneTags { get; }

        public ScribanPallet Pallet { get; }

        public ScribanCrate(Crate crate, ScribanPallet pallet = null)
        {
            Title = crate.Title;
            Description = crate.Description;
            Redacted = crate.Redacted;
            Barcode = crate.Barcode.ID;
            Unlockable = crate.Unlockable;
            if (crate.Tags == null)
                Tags = [];
            else
                Tags = [.. crate.Tags];
            Pallet = pallet ?? new ScribanPallet(crate.Pallet);

            if (crate.BoneTags == null || crate.BoneTags.Tags == null)
            {
                BoneTags = [];
            }
            else
            {
                List<ScribanBoneTag> scribanBoneTags = [];
                crate.BoneTags.Tags.ForEach((Action<BoneTagReference>)(c => scribanBoneTags.Add(new ScribanBoneTag(c.DataCard, Pallet))));
                BoneTags = [.. scribanBoneTags];
            }

            if (crate.GetIl2CppType().Name == nameof(SpawnableCrate))
                Type = CrateType.Spawnable;
            else if (crate.GetIl2CppType().Name == nameof(AvatarCrate))
                Type = CrateType.Avatar;
            else if (crate.GetIl2CppType().Name == nameof(LevelCrate))
                Type = CrateType.Level;
            else if (crate.GetIl2CppType().Name == nameof(VFXCrate))
                Type = CrateType.VFX;
            else
                throw new ArgumentOutOfRangeException($"Crate type {crate.GetIl2CppType().Name} is not supported.");
        }

        public enum CrateType
        {
            Spawnable,
            Avatar,
            Level,
            VFX
        }
    }

    public class ScribanPallet
    {
        public string Title { get; }
        public string Description { get; }
        public string Author { get; }
        public string Barcode { get; }

        public string[] Tags { get; }

        public bool Redacted { get; }

        public bool Unlockable { get; }

        public string Version { get; }

        public string SDKVersion { get; }

        public ScriptArray<ScribanCrate> Crates { get; }

        public ScriptArray<ScribanChangeLog> ChangeLogs { get; }

        public ScriptArray<ScribanDataCard> DataCards { get; }

        public string[] Dependencies { get; }

        public ScribanPallet(Pallet pallet)
        {
            Barcode = pallet.Barcode.ID;
            Unlockable = pallet.Unlockable;
            Redacted = pallet.Redacted;
            Title = pallet.Title;
            if (pallet.Tags == null)
                Tags = [];
            else
                Tags = pallet.Tags.ToArray();
            Version = pallet.Version;

            if (pallet.Crates == null)
            {
                Crates = [];
            }
            else
            {
                List<ScribanCrate> scribanCrates = [];
                pallet.Crates.ForEach((Action<Crate>)(c => scribanCrates.Add(new ScribanCrate(c, this))));
                Crates = [.. scribanCrates];
            }

            Author = pallet.Author;
            Description = pallet.Description;
            SDKVersion = pallet.SDKVersion;

            if (pallet.ChangeLogs == null)
            {
                ChangeLogs = [];
            }
            else
            {
                List<ScribanChangeLog> scribanChangeLogs = [];
                foreach (var c in pallet.ChangeLogs)
                    scribanChangeLogs.Add(new ScribanChangeLog(c));
                ChangeLogs = [.. scribanChangeLogs];
            }

            if (pallet.DataCards == null)
            {
                DataCards = [];
            }
            else
            {
                List<ScribanDataCard> scribanDataCards = [];
                pallet.DataCards.ForEach((Action<DataCard>)(c => scribanDataCards.Add(new ScribanDataCard(c, this))));
                DataCards = [.. scribanDataCards];
            }

            if (pallet.PalletDependencies == null)
            {
                Dependencies = [];
            }
            else
            {
                List<string> dependencies = [];
                pallet.PalletDependencies.ForEach((Action<PalletReference>)(p => dependencies.Add(p.Barcode.ID)));
                Dependencies = [.. dependencies];
            }
        }
    }

    public class ScribanChangeLog(Pallet.ChangeLog changelog)
    {
        public string Title { get; } = changelog.title;

        public string Version { get; } = changelog.version;

        public string Text { get; } = changelog.text;
    }

    public class ScribanDataCard(DataCard dataCard, ScribanPallet pallet = null)
    {
        public string Title { get; } = dataCard.Title;
        public string Description { get; } = dataCard.Description;

        public string Barcode { get; } = dataCard.Barcode.ID;

        public bool Redacted { get; } = dataCard.Redacted;

        public bool Unlockable { get; } = dataCard.Unlockable;

        public ScribanPallet Pallet { get; } = pallet ?? new ScribanPallet(dataCard.Pallet);
    }

    public class ScribanBoneTag(BoneTag boneTag, ScribanPallet pallet = null)
    {
        public string Title { get; } = boneTag.Title;
        public string Description { get; } = boneTag.Description;

        public string Barcode { get; } = boneTag.Barcode.ID;

        public bool Redacted { get; } = boneTag.Redacted;

        public bool Unlockable { get; } = boneTag.Unlockable;

        public ScribanPallet Pallet { get; } = pallet ?? new ScribanPallet(boneTag.Pallet);
    }

    public class ScribanAmmo : ScriptObject
    {
        public static int GetAmmo(string type)
            => AmmoInventory.Instance?._groupCounts[type] ?? 0;

        public static int Light => GetAmmo("light");

        public static int Medium => GetAmmo("medium");

        public static int Heavy => GetAmmo("heavy");
    }

    public class ScribanPlayer : ScriptObject
    {
        public static ScribanCrate Avatar => Player.RigManager?.AvatarCrate?.Crate != null ? new ScribanCrate(Player.RigManager?.AvatarCrate?.Crate) : null;

        public static ScribanCrate LeftHand => Core.GetInHand(Handedness.LEFT) != null ? new ScribanCrate(Core.GetInHand(Handedness.LEFT)) : null;

        public static ScribanCrate RightHand => Core.GetInHand(Handedness.RIGHT) != null ? new ScribanCrate(Core.GetInHand(Handedness.RIGHT)) : null;

        public static float Health => Player.RigManager?.health?.curr_Health ?? 0;

        public static float MaxHealth => Player.RigManager?.health?.max_Health ?? 0;

        public static float HealthPercentange => (Health / MaxHealth) * 100;
    }

    public class ScribanGame : ScriptObject
    {
        public static ScribanCrate Level => SceneStreamer.Session?.Level != null ? new ScribanCrate(SceneStreamer.Session?.Level) : null;

        public static string LevelName => Core.CleanLevelName();

        public static string MLVersion => AppDomain.CurrentDomain?.GetAssemblies()?.FirstOrDefault(x => x.GetName().Name == "MelonLoader")?.GetName()?.Version?.ToString() ?? "N/A";

        public static int FPS => Fps.FramesPerSecond;

        public static string OperatingSystem => SystemInfo.operatingSystem;

        public static int ModsCount
        {
            get
            {
                var modsCount = 0;
                if (AssetWarehouse.ready && AssetWarehouse.Instance != null)
                    modsCount = (AssetWarehouse.Instance.PalletCount() - AssetWarehouse.Instance.gamePallets.Count);

                return modsCount;
            }
        }

        public static int CodeModsCount => Core.RegisteredMelons.Count;
    }

    public class ScribanUtils : ScriptObject
    {
        public static ScribanPallet GetPallet(string barcode)
        {
            if (AssetWarehouse.Instance.TryGetPallet(new Barcode(barcode), out var pallet))
                return new ScribanPallet(pallet);

            return null;
        }

        public static string CleanString(string str)
            => Core.RemoveUnityRichText(str);
    }
}