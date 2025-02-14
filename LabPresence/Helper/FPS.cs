using System;

namespace LabPresence.Helper
{
    internal static class FPS
    {
        internal static int FramesPerSecond { get; private set; }

        internal static int FramesRendered { get; private set; }

        internal static DateTime LastTime { get; private set; }

        internal static void OnUpdate()
        {
            FramesRendered++;

            if ((DateTime.Now - LastTime).TotalSeconds >= 1)
            {
                FramesPerSecond = FramesRendered;
                FramesRendered = 0;
                LastTime = DateTime.Now;
            }
        }
    }
}