using System;
using System.Collections.Generic;
using System.Linq;

namespace LabPresence
{
    public static class Gamemodes
    {
        private static readonly List<Gamemode> _Gamemodes = [];

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

        public static void RegisterGamemode(string barcode, Func<string> customToolTip)
            => RegisterGamemode(new(barcode, customToolTip: customToolTip));

        public static void RegisterGamemode(string barcode, Func<Timestamp> overrideTime)
            => RegisterGamemode(new(barcode, overrideTime: overrideTime));

        public static void RegisterGamemode(string barcode, Func<string> customToolTip, Func<Timestamp> overrideTime)
            => RegisterGamemode(new(barcode, customToolTip: customToolTip, overrideTime: overrideTime));

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

        public static bool UnregisterGamemode(this Gamemode gamemode)
            => UnregisterGamemode(gamemode?.Barcode);

        public static bool IsGamemodeRegistered(string barcode)
            => _Gamemodes.Any(x => x.Barcode == barcode);

        public static bool IsGamemodeRegistered(this Gamemode gamemode)
            => IsGamemodeRegistered(gamemode?.Barcode);

        public static Gamemode GetGamemode(string barcode)
            => _Gamemodes.FirstOrDefault(x => x.Barcode == barcode);

        public static int GetGamemodeCount()
            => _Gamemodes.Count;

        public static string[] GetGamemodeBarcodes()
        {
            List<string> barcodes = [];
            _Gamemodes.ForEach(x => barcodes.Add(x.Barcode));
            return [.. barcodes];
        }

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

        public static string GetToolTipValue(this Gamemode gamemode)
            => GetToolTipValue(gamemode?.Barcode);

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

        public static Timestamp GetOverrideTime(this Gamemode gamemode)
            => GetOverrideTime(gamemode?.Barcode);
    }

    public class Gamemode(string barcode, Func<string> customToolTip = null, Func<Timestamp> overrideTime = null)
    {
        public string Barcode { get; } = barcode;

        public Func<string> CustomToolTip { get; } = customToolTip;

        public Func<Timestamp> OverrideTime { get; } = overrideTime;
    }
}