using System;
using System.Collections;

using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Marrow.Utilities;
using Il2CppSLZ.Marrow.Warehouse;

using MelonLoader;

namespace LabPresence.Helper
{
    /// <summary>
    /// Class that contains events for level load, unload, loading
    /// </summary>
    public static class LevelHooks
    {
        /// <summary>
        /// The current level
        /// </summary>
        public static LevelCrate CurrentLevel => SceneStreamer.Session?.Level ?? null;

        /// <summary>
        /// Triggered when a level gets loaded
        /// </summary>
        public static Action<LevelCrate> OnLevelLoaded { get; set; }

        /// <summary>
        /// Triggered when a level is loading
        /// </summary>
        public static Action<LevelCrate> OnLevelLoading { get; set; }

        /// <summary>
        /// Triggered when a level gets unloaded
        /// </summary>
        public static Action<LevelCrate> OnLevelUnloaded { get; set; }

        internal static LastStatus lastStatus;

        /*
        internal static void Init()
        {
            SceneStreamer.doAnyLevelLoad += (System.Action)(() => OnLevelLoaded?.Invoke(SceneStreamer.Session.Level));
            SceneStreamer.doAnyLevelUnload += (System.Action)(() =>
            {
                OnLevelUnloaded?.Invoke(SceneStreamer.Session.Level);
                MelonCoroutines.Start(WaitForLoading());
            });
        }

        private static IEnumerator WaitForLoading()
        {
            var level = SceneStreamer.Session.Level;
            while (SceneStreamer.Session?.Status != StreamStatus.LOADING)
                yield return null;

            OnLevelUnloaded?.Invoke(level);
        }
        */

        // TODO: Improve to not check every frame for changes
        internal static void OnUpdate()
        {
            if (!MarrowGame.IsInitialized)
                return;

            if (SceneStreamer.Session == null)
                return;

            if (SceneStreamer.Session.Level == null)
                return;

            if (lastStatus?.UpToDate(SceneStreamer.Session.Level, SceneStreamer.Session.Status) != true)
            {
                try
                {
                    if (SceneStreamer.Session.Status == StreamStatus.DONE)
                        OnLevelLoaded?.Invoke(SceneStreamer.Session.Level);
                    else if (SceneStreamer.Session.Status == StreamStatus.LOADING)
                        OnLevelLoading?.Invoke(SceneStreamer.Session.Level);

                    if (SceneStreamer.Session.Status != StreamStatus.DONE && lastStatus?.Status == StreamStatus.DONE)
                        OnLevelUnloaded?.Invoke(lastStatus?.Level);
                }
                finally
                {
                    lastStatus = new(SceneStreamer.Session.Status, SceneStreamer.Session.Level);
                }
            }
        }
    }

    internal class LastStatus(StreamStatus status, LevelCrate level)
    {
        internal StreamStatus Status { get; set; } = status;

        internal LevelCrate Level { get; set; } = level;

        internal bool UpToDate(string barcode, StreamStatus status)
            => Level?.Barcode?.ID == barcode && Status == status;

        internal bool UpToDate(LevelCrate level, StreamStatus status)
            => Level?.Barcode?.ID == level?.Barcode?.ID && Status == status;
    }
}