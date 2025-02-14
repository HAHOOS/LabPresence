using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LabPresence
{
    public static class Placeholders
    {
        private static readonly Dictionary<string, Func<string>> _Placeholders = [];

        public static void AddPlaceholder(string name, Func<string> function)
            => _Placeholders[name] = function;

        public static void RemovePlaceholder(string name)
            => _Placeholders.Remove(name);

        public static bool HasPlaceholder(string name)
            => _Placeholders.ContainsKey(name);

        public static int GetPlaceholderCount()
            => _Placeholders.Count;

        public static string[] GetPlaceholderNames()
            => [.. _Placeholders.Keys];

        public static string ApplyPlaceholders(this string text)
        {
            foreach (var placeholder in _Placeholders)
            {
                Regex regex = new(@$"(?'escaped'\\%{Regex.Escape(placeholder.Key)}\\%)|(?'found'%{Regex.Escape(placeholder.Key)}%)");
                string match(Match match)
                {
                    if (match.Success)
                    {
                        if (match.Groups.ContainsKey("escaped") &&
                            match.Groups["escaped"].Success &&
                            !string.IsNullOrWhiteSpace(match.Groups["escaped"].Value))
                        {
                            return $"%{placeholder.Key}%";
                        }
                        else if (match.Groups.ContainsKey("found") &&
                            match.Groups["found"].Success &&
                            !string.IsNullOrWhiteSpace(match.Groups["found"].Value))
                        {
                            try
                            {
                                var replaced = placeholder.Value.Invoke();
                                return replaced;
                            }
                            catch (Exception ex)
                            {
                                Core.Logger.Error($"Placeholder '{placeholder.Key}' threw an exception:\n{ex}");
                            }
                        }
                    }
                    return string.Empty;
                }
                text = regex.Replace(text, match);
            }
            return text;
        }
    }
}