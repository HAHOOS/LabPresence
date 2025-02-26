using System;

using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Marrow.Utilities;
using Il2CppSLZ.Marrow.Warehouse;

namespace LabPresence.Helper
{
    public static class LevelHooks
    {
        public static LevelCrate CurrentLevel => SceneStreamer.Session?.Level ?? null;

        public static Action<LevelCrate> OnLevelLoaded { get; set; }

        public static Action<LevelCrate> OnLevelLoading { get; set; }

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

            if (lastStatus?.UpToDate(SceneStreamer.Session.Level.Barcode.ID, SceneStreamer.Session.Status) != true)
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
    }
}