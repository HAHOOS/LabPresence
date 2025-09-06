using System;
using System.Collections.Generic;
using System.Linq;

using LabPresence.Managers;

namespace LabPresence
{
    /// <summary>
    /// Class responsible for managing Fusion Gamemode support
    /// </summary>
    public static class Gamemodes
    {
        private static readonly List<Gamemode> _Gamemodes = [];

        /// <summary>
        /// Registers a new <see cref="Gamemode"/>
        /// </summary>
        /// <param name="gamemode">The gamemode to register</param>
        public static void RegisterGamemode(this Gamemode gamemode)
        {
            ArgumentNullException.ThrowIfNull(gamemode, nameof(gamemode));

            if (_Gamemodes.Contains(gamemode))
                throw new ArgumentException("Gamemode is already registered!");

            if (string.IsNullOrWhiteSpace(gamemode.Barcode))
                throw new ArgumentNullException(nameof(gamemode), "The barcode cannot be empty or null!");

            if (_Gamemodes.Any(x => x.Barcode == gamemode.Barcode))
                throw new ArgumentException("A gamemode with the same barcode is already registered!");

            if (gamemode.CustomToolTip == null && gamemode.OverrideTime == null)
                throw new ArgumentException("The gamemode needs to have a custom tooltip and/or override time");

            _Gamemodes.Add(gamemode);
        }

        /// <summary>
        /// Registers a new <see cref="Gamemode"/>
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <param name="customToolTip"><inheritdoc cref="Gamemode.CustomToolTip"/></param>
        public static void RegisterGamemode(string barcode, Func<string> customToolTip)
            => RegisterGamemode(new(barcode, customToolTip: customToolTip));

        /// <summary>
        /// Registers a new <see cref="Gamemode"/>
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <param name="minimumDelay"><inheritdoc cref="Gamemode.MinimumDelay"/></param>
        /// <param name="customToolTip"><inheritdoc cref="Gamemode.CustomToolTip"/></param>
        public static void RegisterGamemode(string barcode, float minimumDelay, Func<string> customToolTip)
            => RegisterGamemode(new(barcode, minimumDelay: minimumDelay, customToolTip: customToolTip));

        /// <summary>
        /// Registers a new <see cref="Gamemode"/>
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <param name="overrideTime"><inheritdoc cref="Gamemode.OverrideTime"/></param>
        public static void RegisterGamemode(string barcode, Func<Timestamp> overrideTime)
            => RegisterGamemode(new(barcode, overrideTime: overrideTime));

        /// <summary>
        /// Registers a new <see cref="Gamemode"/>
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <param name="customToolTip"><inheritdoc cref="Gamemode.CustomToolTip"/></param>
        /// <param name="overrideTime"><inheritdoc cref="Gamemode.OverrideTime"/></param>
        public static void RegisterGamemode(string barcode, Func<string> customToolTip, Func<Timestamp> overrideTime)
            => RegisterGamemode(new(barcode, customToolTip: customToolTip, overrideTime: overrideTime));

        /// <summary>
        /// Registers a new <see cref="Gamemode"/>
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <param name="minimumDelay"><inheritdoc cref="Gamemode.MinimumDelay"/></param>
        /// <param name="customToolTip"><inheritdoc cref="Gamemode.CustomToolTip"/></param>
        /// <param name="overrideTime"><inheritdoc cref="Gamemode.OverrideTime"/></param>
        public static void RegisterGamemode(string barcode, float minimumDelay, Func<string> customToolTip, Func<Timestamp> overrideTime)
            => RegisterGamemode(new(barcode, minimumDelay: minimumDelay, customToolTip: customToolTip, overrideTime: overrideTime));

        /// <summary>
        /// Unregister a <see cref="Gamemode"/>
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <returns><see langword="true"/> if found and removed successfully, otherwise <see langword="false"/></returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided barcode is null or empty</exception>
        public static bool UnregisterGamemode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentNullException(nameof(barcode), "The barcode cannot be null or empty!");

            if (_Gamemodes.Count == 0)
                return false;

            var index = _Gamemodes.FindIndex(x => x.Barcode == barcode);
            if (index == -1)
                return false;

            _Gamemodes.RemoveAt(index);

            return true;
        }

        /// <summary>
        /// Unregister a <see cref="Gamemode"/>
        /// </summary>
        /// <param name="gamemode">The <see cref="Gamemode"/> to unregister</param>
        /// <returns><see langword="true"/> if found and removed successfully, otherwise <see langword="false"/></returns>
        public static bool UnregisterGamemode(this Gamemode gamemode)
            => UnregisterGamemode(gamemode?.Barcode);

        /// <summary>
        /// Checks if a <see cref="Gamemode"/> is registered
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <returns><see langword="true"/> if registered, otherwise <see langword="false"/></returns>
        public static bool IsGamemodeRegistered(string barcode)
            => _Gamemodes.Any(x => x.Barcode == barcode);

        /// <summary>
        /// Checks if a <see cref="Gamemode"/> is registered
        /// </summary>
        /// <param name="gamemode"><see cref="Gamemode"/> to check if registered</param>
        /// <returns><see langword="true"/> if registered, otherwise <see langword="false"/></returns>
        public static bool IsGamemodeRegistered(this Gamemode gamemode)
            => IsGamemodeRegistered(gamemode?.Barcode);

        /// <summary>
        /// Get a <see cref="Gamemode"/> from its barcode
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <returns><see cref="Gamemode"/> if found, otherwise <see langword="null"/></returns>
        public static Gamemode GetGamemode(string barcode)
            => _Gamemodes.FirstOrDefault(x => x.Barcode == barcode);

        /// <summary>
        /// Get the amount of all registered gamemodes
        /// </summary>
        public static int GetGamemodeCount()
            => _Gamemodes.Count;

        /// <summary>
        /// Get the barcodes of all registered gamemodes
        /// </summary>
        public static string[] GetGamemodeBarcodes()
        {
            List<string> barcodes = [];
            _Gamemodes.ForEach(x => barcodes.Add(x.Barcode));
            return [.. barcodes];
        }

        /// <summary>
        /// Get the ToolTip for a <see cref="Gamemode"/> with specified barcode
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <returns>The returned tooltip, <see cref="string.Empty"/> when an exception occurs or gamemode was not found</returns>
        public static string GetToolTipValue(string barcode)
        {
            var registered = _Gamemodes.FirstOrDefault(x => x.Barcode == barcode);
            if (registered == null || registered.CustomToolTip == null)
                return string.Empty;

            string ret;

            try
            {
                ret = registered.CustomToolTip?.Invoke();
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"An unexpected error has occurred while trying to get value of tooltip of the gamemode with barcode '{barcode}', exception:\n{ex}");
                ret = string.Empty;
            }

            return ret;
        }

        /// <summary>
        /// Get the ToolTip for a <see cref="Gamemode"/>
        /// </summary>
        /// <param name="gamemode"><see cref="Gamemode"/> to get the tooltip value of</param>
        /// <returns>The returned tooltip, <see cref="string.Empty"/> when an exception occurs or gamemode was not found</returns>
        public static string GetToolTipValue(this Gamemode gamemode)
            => GetToolTipValue(gamemode?.Barcode);

        /// <summary>
        /// Get the override time of a <see cref="Gamemode"/> with the specified barcode
        /// </summary>
        /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
        /// <returns>The returned override time, <see langword="null"/> when an exception occurs or gamemode was not found</returns>
        public static Timestamp GetOverrideTime(string barcode)
        {
            var registered = _Gamemodes.FirstOrDefault(x => x.Barcode == barcode);
            if (registered == null || registered.OverrideTime == null)
                return null;

            Timestamp ret;

            try
            {
                ret = registered.OverrideTime?.Invoke();
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"An unexpected error has occurred while trying to get value of tooltip of the gamemode with barcode '{barcode}', exception:\n{ex}");
                ret = null;
            }

            return ret;
        }

        /// <summary>
        /// Get the override time of a <see cref="Gamemode"/>
        /// </summary>
        /// <param name="gamemode">The <see cref="Gamemode"/> to get the override time of</param>
        /// <returns>The returned override time, <see langword="null"/> when an exception occurs or gamemode was not found</returns>
        public static Timestamp GetOverrideTime(this Gamemode gamemode)
            => GetOverrideTime(gamemode?.Barcode);
    }

    /// <summary>
    /// Class that contains data about a gamemode to be used in <see cref="Gamemodes"/>
    /// </summary>
    /// <param name="barcode"><inheritdoc cref="Gamemode.Barcode"/></param>
    /// <param name="minimumDelay"><inheritdoc cref="Gamemode.MinimumDelay"/></param>
    /// <param name="customToolTip"><inheritdoc cref="Gamemode.CustomToolTip"/></param>
    /// <param name="overrideTime"><inheritdoc cref="Gamemode.OverrideTime"/></param>
    public class Gamemode(string barcode, float minimumDelay = 0, Func<string> customToolTip = null, Func<Timestamp> overrideTime = null)
    {
        /// <summary>
        /// The barcode of the gamemode
        /// </summary>
        public string Barcode { get; } = barcode;

        /// <summary>
        /// The minimum delay of the RPC refresh when <see cref="CustomToolTip"/> is shown
        /// </summary>
        public float MinimumDelay { get; } = minimumDelay;

        /// <summary>
        /// The function that returns a <see langword="string"/> which will be displayed on the small icon tooltip
        /// <para>Returning <see langword="null"/> or an empty string will cause the tooltip to not be shown</para>
        /// </summary>
        public Func<string> CustomToolTip { get; } = customToolTip;

        /// <summary>
        /// The function that overrides the <see cref="RichPresenceManager.Timestamp"/> when the gamemode is active
        /// <para>Returning <see langword="null"/> will cause the override to not happen</para>
        /// </summary>
        public Func<Timestamp> OverrideTime { get; } = overrideTime;
    }
}