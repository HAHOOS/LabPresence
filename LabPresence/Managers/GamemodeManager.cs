using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace LabPresence.Managers
{
    public static class GamemodeManager
    {
        private static readonly Dictionary<string, GamemodeOverrides> _Gamemodes = [];

        public static IReadOnlyDictionary<string, GamemodeOverrides> Gamemodes => _Gamemodes;

        public static void RegisterGamemode(this GamemodeOverrides gamemode, string barcode)
        {
            ArgumentNullException.ThrowIfNull(gamemode);

            if (_Gamemodes.ContainsKey(barcode))
                throw new ArgumentException("Gamemode is already registered!");

            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentNullException(nameof(gamemode), "The barcode cannot be empty or null!");

            if (IsGamemodeRegistered(barcode))
                throw new ArgumentException("A gamemode with the same barcode is already registered!");

            if (gamemode.CustomToolTip == null && gamemode.OverrideTime == null)
                throw new ArgumentException("The gamemode needs to have a custom tooltip and/or override time");

            _Gamemodes.Add(barcode, gamemode);
        }

        public static void RegisterGamemode(string barcode, Func<string> customToolTip)
            => RegisterGamemode(new(customToolTip: customToolTip), barcode);

        public static void RegisterGamemode(string barcode, Func<Timestamp> overrideTime)
            => RegisterGamemode(new(overrideTime: overrideTime), barcode);

        public static void RegisterGamemode(string barcode, Func<string> customToolTip, string smallImage)
           => RegisterGamemode(new(smallImage: smallImage, customToolTip: customToolTip), barcode);

        public static void RegisterGamemode(string barcode, Func<Timestamp> overrideTime, string smallImage)
            => RegisterGamemode(new(smallImage: smallImage, overrideTime: overrideTime), barcode);

        public static void RegisterGamemode(string barcode, Func<string> customToolTip, Func<Timestamp> overrideTime)
            => RegisterGamemode(new(customToolTip: customToolTip, overrideTime: overrideTime), barcode);

        public static void RegisterGamemode(string barcode, Func<string> customToolTip, Func<Timestamp> overrideTime, string smallImage)
            => RegisterGamemode(new(smallImage: smallImage, customToolTip: customToolTip, overrideTime: overrideTime), barcode);

        public static bool UnregisterGamemode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentNullException(nameof(barcode), "The barcode cannot be null or empty!");

            if (_Gamemodes.Count == 0)
                return false;

            return _Gamemodes.Remove(barcode);
        }

        public static bool IsGamemodeRegistered(string barcode)
            => _Gamemodes.ContainsKey(barcode);

        public static GamemodeOverrides GetGamemode(string barcode)
            => _Gamemodes.ContainsKey(barcode) ? _Gamemodes[barcode] : null;

        public static int GetGamemodeCount()
            => _Gamemodes.Count;

        public static string[] GetGamemodeBarcodes()
            => [.. _Gamemodes.Keys];

        public static string GetToolTipValue(string barcode)
        {
            var registered = GetGamemode(barcode);
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

        public static Timestamp GetOverrideTime(string barcode)
        {
            var registered = GetGamemode(barcode);
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

        private static JsonDocument KnownGamemodesCache;

        public static string GetGamemodeKey(string barcode)
        {
            if (IsGamemodeRegistered(barcode))
            {
                var key = GetGamemode(barcode)?.SmallImage;
                if (!string.IsNullOrWhiteSpace(key))
                    return key;
            }

            return GetRemoteGamemodeKey(barcode);
        }

        public static string GetRemoteGamemodeKey(string barcode)
        {
            try
            {
                const string knownGamemodes = "https://raw.githubusercontent.com/HAHOOS/LabPresence/refs/heads/master/Data/gamemodes.json";
                if (KnownGamemodesCache == null)
                {
                    var client = new HttpClient();
                    var req = client.GetAsync(knownGamemodes);
                    req.Wait();
                    if (req.IsCompletedSuccessfully && req.Result.IsSuccessStatusCode)
                    {
                        var content = req.Result.Content.ReadAsStringAsync();
                        content.Wait();
                        if (content.IsCompletedSuccessfully)
                        {
                            KnownGamemodesCache = JsonDocument.Parse(content.Result);
                        }
                    }
                }
                if (KnownGamemodesCache != null && KnownGamemodesCache.RootElement.TryGetProperty(barcode, out JsonElement val))
                    return val.GetString();
            }
            catch (Exception e)
            {
                Core.Logger.Error($"An unexpected error has occurred while trying to remotely get a key for the gamemode, defaulting to unknown key. Exception:\n{e}");
            }
            return "unknown_gamemode";
        }
    }

    public class GamemodeOverrides(string smallImage = null, Func<string> customToolTip = null, Func<Timestamp> overrideTime = null)
    {
        public string SmallImage { get; } = smallImage;

        public Func<string> CustomToolTip { get; } = customToolTip;

        public Func<Timestamp> OverrideTime { get; } = overrideTime;
    }
}