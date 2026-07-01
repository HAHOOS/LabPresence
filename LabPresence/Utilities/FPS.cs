using System;

using UnityEngine;

namespace LabPresence.Utilities
{
    public static class Fps
    {
        public static int FramesPerSecond { get; private set; }

        internal static void OnUpdate()
            => FramesPerSecond = (int)(1f / Time.unscaledDeltaTime);
    }
}