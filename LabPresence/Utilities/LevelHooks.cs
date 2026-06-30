using System;

using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Marrow.Warehouse;

using HarmonyLib;

using BoneLib;

namespace LabPresence.Utilities
{
    [HarmonyPatch(typeof(StreamSession))]
    public static class LevelHooks
    {
        public static LevelCrate CurrentLevel => SceneStreamer.Session?.Level ?? null;

        public static Action<LevelCrate> OnLevelLoaded { get; set; }

        public static Action<LevelCrate> OnLevelLoading { get; set; }

        public static Action OnLevelUnloaded { get; set; }

        private static StreamStatus _lastStatus;

        public static void Setup()
        {
            Hooking.OnLevelLoaded += (_) => OnLevelLoaded?.Invoke(CurrentLevel);
            Hooking.OnLevelUnloaded += () => OnLevelUnloaded?.Invoke();
        }

        // This only seems to work for the LOADING status
        [HarmonyPatch(nameof(StreamSession.Load))]
        [HarmonyPatch(nameof(StreamSession.Level), MethodType.Setter)]
        [HarmonyPatch(nameof(StreamSession.Status), MethodType.Setter)]
        [HarmonyPostfix]
        public static void OnLoading(StreamSession __instance)
        {
            if (__instance == null)
                return;

            if (_lastStatus == __instance.Status)
                return;

            _lastStatus = __instance.Status;
            if (__instance.Status == StreamStatus.LOADING)
                OnLevelLoading.Invoke(__instance.Level);
        }
    }

    [HarmonyPatch(typeof(SceneStreamer))]
    public static class SceneStreamerPatches
    {
        [HarmonyPatch(nameof(SceneStreamer.Load))]
        [HarmonyPatch(nameof(SceneStreamer.LoadAsync))]
        [HarmonyPostfix]
        public static void OnLoading()
            => LevelHooks.OnLoading(SceneStreamer.Session);
    }
}