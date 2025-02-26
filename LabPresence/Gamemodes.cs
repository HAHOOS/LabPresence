using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabPresence
{
    public static class Gamemodes
    {
        private static Dictionary<string, Func<string>> _Gamemodes = [];

        public static void RegisterTooltip(string barcode, Func<string> tooltipValue)
        {
            if (_Gamemodes.ContainsKey(barcode))
                throw new ArgumentException("The gamemode tooltip for the provided barcode already exists");

            _Gamemodes.Add(barcode, tooltipValue);
        }

        public static void UnregisterTooltip(string barcode)
        {
            if (!_Gamemodes.ContainsKey(barcode))
                throw new ArgumentException("The gamemode tooltip for the provided barcode does not exist");

            _Gamemodes.Remove(barcode);
        }

        public static int GetTooltipCount()
            => _Gamemodes.Count;

        public static bool HasTooltip(string barcode)
            => _Gamemodes.ContainsKey(barcode);

        public static string GetValue(string barcode)
        {
            if (!HasTooltip(barcode))
                throw new ArgumentException("Tried to get value of a tooltip that doesn't exist!");

            string ret;

            try
            {
                ret = _Gamemodes[barcode]?.Invoke();
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"An unexpected error has occurred while trying to get value of tooltip of the gamemode with barcode '{barcode}', exception:\n{ex}");
                ret = string.Empty;
            }

            return ret;
        }
    }
}