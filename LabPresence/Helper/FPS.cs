using System;

namespace LabPresence.Helper
{
    /// <summary>
    /// Class responsible for checking the Frames Per Second of the game
    /// </summary>
    public static class FPS
    {
        /// <summary>
        /// Frames per second value
        /// </summary>
        public static int FramesPerSecond { get; private set; }

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