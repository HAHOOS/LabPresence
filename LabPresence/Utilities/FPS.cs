using System;

using UnityEngine;

namespace LabPresence.Utilities
{
    public static class Fps
    {
        public static int FramesPerSecond { get; private set; }

        internal static int FramesRendered { get; private set; }

        internal static DateTime LastTime { get; private set; }

        private static float updateTime;

        internal static void OnUpdate()
        {
            FramesRendered++;
            updateTime += Time.deltaTime;
            if (updateTime >= 1)
            {
                updateTime = 0;
                FramesPerSecond = FramesRendered;
                FramesRendered = 0;
                LastTime = DateTime.Now;
            }
        }
    }
}